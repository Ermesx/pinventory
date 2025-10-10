namespace Pinventory.Notifications;

public sealed class NotificationInbox
{
    public Guid UserId { get; private set; }
    private readonly List<Notification> _notifications = new();
    public IReadOnlyCollection<Notification> Notifications => _notifications;
    private NotificationInbox() { }
    public NotificationInbox(Guid userId) { UserId = userId; }
    public void Enqueue(Notification n) => _notifications.Add(n);

    public void Acknowledge(Guid notificationId)
    {
        var n = _notifications.Find(x => x.Id == notificationId);
        if (n is null) return;
        n.AcknowledgedAt = DateTimeOffset.UtcNow;
    }
}