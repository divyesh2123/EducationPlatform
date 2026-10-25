using EventBus.Abstractions;
using Microsoft.EntityFrameworkCore;
using NotificationService.API.Data;
using NotificationService.API.Models;

namespace NotificationService.API.Services;

public interface INotificationSender
{
    Task SendAsync(IntegrationEvent @event, string recipient, string subject, string body);
}

/// <summary>
/// Stores the notification and "sends" it. A real system would call an SMTP/SMS provider here;
/// this example just logs so it runs without external accounts.
/// </summary>
public class NotificationSender(NotificationDbContext db, ILogger<NotificationSender> logger) : INotificationSender
{
    public async Task SendAsync(IntegrationEvent @event, string recipient, string subject, string body)
    {
        if (await db.Notifications.AnyAsync(n => n.IntegrationEventId == @event.Id && n.Recipient == recipient))
        {
            logger.LogInformation("Event {EventId} already handled for {Recipient}, skipping duplicate", @event.Id, recipient);
            return;
        }

        db.Notifications.Add(new Notification
        {
            IntegrationEventId = @event.Id,
            EventType = @event.GetType().Name,
            Recipient = recipient,
            Subject = subject,
            Body = body
        });
        await db.SaveChangesAsync();

        logger.LogInformation("EMAIL to {Recipient} | {Subject} | {Body}", recipient, subject, body);
    }
}
