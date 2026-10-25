namespace NotificationService.API.Models;

public class Notification
{
    public int Id { get; set; }

    /// <summary>Id of the integration event that produced this notification (used for idempotency).</summary>
    public Guid IntegrationEventId { get; set; }

    public string EventType { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
