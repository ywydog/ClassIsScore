namespace ClassIsScore.Models;

/// <summary>
/// IPC 消息接收事件参数
/// </summary>
public class IpcMessageReceivedEventArgs : EventArgs
{
    /// <summary>
    /// 命令名称
    /// </summary>
    public string Command { get; init; } = string.Empty;

    /// <summary>
    /// 命令参数（可选）
    /// </summary>
    public string? Payload { get; init; }

    /// <summary>
    /// 消息接收时间
    /// </summary>
    public DateTime ReceivedAt { get; init; } = DateTime.Now;
}
