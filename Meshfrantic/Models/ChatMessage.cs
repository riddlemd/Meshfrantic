namespace Meshfrantic.Models;

public class ChatMessage
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public uint FromNodeId { get; init; }
    public string FromNodeName { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public bool IsOwn { get; init; }
    public int ChannelIndex { get; init; }
    public uint? DestNodeId { get; init; }
    public uint PacketId { get; init; }
    public bool IsAcked { get; set; }
    public bool IsFailed { get; set; }
}
