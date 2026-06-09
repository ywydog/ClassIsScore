using ClassIsScore.Models;

namespace ClassIsScore.Services.Abstractions;

/// <summary>
/// IPC 通信服务接口，支持与 ClassIsland 及其他外部进程通信
/// </summary>
public interface IIpcService
{
    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 启动 IPC 服务
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// 停止 IPC 服务
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// 发送命令到 IPC 通道
    /// </summary>
    /// <param name="command">命令名称</param>
    /// <param name="payload">命令参数（可选）</param>
    Task SendCommandAsync(string command, string? payload = null);

    /// <summary>
    /// 向所有连接的对方广播消息
    /// </summary>
    /// <param name="id">消息 ID</param>
    Task BroadcastNotificationAsync(string id);

    /// <summary>
    /// 向所有连接的对方广播消息
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="id">消息 ID</param>
    /// <param name="obj">参数对象</param>
    Task BroadcastNotificationAsync<T>(string id, T obj) where T : class;

    /// <summary>
    /// 收到 IPC 消息时触发
    /// </summary>
    event EventHandler<IpcMessageReceivedEventArgs>? MessageReceived;
}
