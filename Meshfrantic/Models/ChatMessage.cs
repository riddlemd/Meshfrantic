namespace Meshfrantic.Models;

public class ChatMessage
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public uint FromNodeId { get; init; }
    public string FromNodeName { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public bool IsOwn { get; init; }
}
