

namespace Pinventory.Notifications;

public sealed class Notification
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Type { get; private set; } = default!;

    public string PayloadJson { get; private set; } = default!;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AcknowledgedAt { get; internal set; }
    private Notification() { }

    public Notification(string type, string payloadJson)
    {
        Type = type;
        PayloadJson = payloadJson;
    }
}