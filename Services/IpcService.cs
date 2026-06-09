using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

/// <summary>
/// IPC 通信服务实现，使用命名管道实现进程间通信
/// 为以后与 ClassIsland 联动做准备
/// </summary>
public class IpcService : IIpcService, IAsyncDisposable
{
    private readonly ILogger<IpcService> _logger;

    /// <summary>
    /// IPC 管道名称
    /// </summary>
    private const string PipeName = "ClassIsScore_IPC";

    /// <summary>
    /// 取消令牌源
    /// </summary>
    private CancellationTokenSource? _cts;

    /// <summary>
    /// 命名管道服务端
    /// </summary>
    private NamedPipeServerStream? _pipeServer;

    /// <summary>
    /// 是否正在运行
    /// </summary>
    private bool _isRunning;

    /// <summary>
    /// 收到 IPC 消息时触发
    /// </summary>
    public event EventHandler<IpcMessageReceivedEventArgs>? MessageReceived;

    public IpcService(ILogger<IpcService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected => _isRunning && _pipeServer?.IsConnected == true;

    /// <summary>
    /// 启动 IPC 服务
    /// </summary>
    public async Task StartAsync()
    {
        if (_isRunning)
        {
            _logger.LogWarning("IPC 服务已在运行中");
            return;
        }

        _cts = new CancellationTokenSource();
        _isRunning = true;
        _logger.LogInformation("IPC 服务已启动，管道名称: {PipeName}", PipeName);

        // 在后台线程中监听管道连接
        _ = Task.Run(() => ListenForConnectionsAsync(_cts.Token), _cts.Token);
    }

    /// <summary>
    /// 停止 IPC 服务
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _cts?.Cancel();

        try
        {
            if (_pipeServer != null)
            {
                await _pipeServer.DisposeAsync();
                _pipeServer = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止 IPC 服务时发生错误");
        }

        _logger.LogInformation("IPC 服务已停止");
    }

    /// <summary>
    /// 发送命令到 IPC 通道（作为客户端连接到服务端发送）
    /// </summary>
    /// <param name="command">命令名称</param>
    /// <param name="payload">命令参数（可选）</param>
    public async Task SendCommandAsync(string command, string? payload = null)
    {
        try
        {
            // 作为客户端连接到已运行的服务端实例
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            await client.ConnectAsync(3000);

            var message = new IpcMessage
            {
                Command = command,
                Payload = payload
            };

            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);

            // 先写入消息长度，再写入消息体
            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            await client.WriteAsync(lengthBytes, 0, lengthBytes.Length);
            await client.WriteAsync(bytes, 0, bytes.Length);
            await client.FlushAsync();

            _logger.LogDebug("已发送 IPC 命令: {Command}", command);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("IPC 客户端连接超时，服务端可能未运行");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送 IPC 命令失败: {Command}", command);
        }
    }

    /// <summary>
    /// 向所有连接的对方广播消息
    /// </summary>
    /// <param name="id">消息 ID</param>
    public Task BroadcastNotificationAsync(string id)
    {
        return SendCommandAsync(id, null);
    }

    /// <summary>
    /// 向所有连接的对方广播消息
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="id">消息 ID</param>
    /// <param name="obj">参数对象</param>
    public Task BroadcastNotificationAsync<T>(string id, T obj) where T : class
    {
        var payload = JsonSerializer.Serialize(obj);
        return SendCommandAsync(id, payload);
    }

    /// <summary>
    /// 监听管道连接
    /// </summary>
    private async Task ListenForConnectionsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _pipeServer = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                // 等待客户端连接
                await _pipeServer.WaitForConnectionAsync(cancellationToken);
                _logger.LogInformation("IPC 客户端已连接");

                // 读取客户端消息
                await ReadMessageAsync(_pipeServer, cancellationToken);

                // 断开当前连接，准备接受下一个
                _pipeServer.Disconnect();
                await _pipeServer.DisposeAsync();
                _pipeServer = null;
            }
            catch (OperationCanceledException)
            {
                // 正常退出
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IPC 监听连接时发生错误");

                // 清理并重试
                try
                {
                    if (_pipeServer != null)
                    {
                        await _pipeServer.DisposeAsync();
                        _pipeServer = null;
                    }
                }
                catch
                {
                    // 忽略清理错误
                }

                // 短暂等待后重试
                try
                {
                    await Task.Delay(1000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 从管道读取消息
    /// </summary>
    private async Task ReadMessageAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        try
        {
            // 读取消息长度（4 字节）
            var lengthBuffer = new byte[4];
            var bytesRead = 0;
            while (bytesRead < 4)
            {
                var read = await pipe.ReadAsync(lengthBuffer.AsMemory(bytesRead, 4 - bytesRead), cancellationToken);
                if (read == 0) return; // 连接已关闭
                bytesRead += read;
            }

            var messageLength = BitConverter.ToInt32(lengthBuffer, 0);
            if (messageLength <= 0 || messageLength > 1024 * 1024) // 限制最大 1MB
            {
                _logger.LogWarning("收到无效的 IPC 消息长度: {Length}", messageLength);
                return;
            }

            // 读取消息体
            var messageBuffer = new byte[messageLength];
            bytesRead = 0;
            while (bytesRead < messageLength)
            {
                var read = await pipe.ReadAsync(messageBuffer.AsMemory(bytesRead, messageLength - bytesRead), cancellationToken);
                if (read == 0) return;
                bytesRead += read;
            }

            var json = Encoding.UTF8.GetString(messageBuffer);
            var message = JsonSerializer.Deserialize<IpcMessage>(json);

            if (message != null)
            {
                _logger.LogDebug("收到 IPC 消息: Command={Command}, Payload={Payload}",
                    message.Command, message.Payload);

                // 触发消息接收事件
                MessageReceived?.Invoke(this, new IpcMessageReceivedEventArgs
                {
                    Command = message.Command,
                    Payload = message.Payload,
                    ReceivedAt = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取 IPC 消息时发生错误");
        }
    }

    /// <summary>
    /// 异步释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts?.Dispose();
    }
}

/// <summary>
/// IPC 消息内部模型，用于序列化/反序列化
/// </summary>
internal class IpcMessage
{
    /// <summary>
    /// 命令名称
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// 命令参数
    /// </summary>
    public string? Payload { get; set; }
}
