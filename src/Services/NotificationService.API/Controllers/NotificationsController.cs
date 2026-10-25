using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationService.API.Data;
using NotificationService.API.Models;

namespace NotificationService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController(NotificationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Notification>>> GetAll() =>
        Ok(await db.Notifications.AsNoTracking().OrderByDescending(n => n.CreatedAt).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Notification>> GetById(int id)
    {
        var notification = await db.Notifications.FindAsync(id);
        return notification is null ? NotFound() : Ok(notification);
    }

    [HttpGet("recipient/{email}")]
    public async Task<ActionResult<IEnumerable<Notification>>> GetByRecipient(string email) =>
        Ok(await db.Notifications.AsNoTracking()
            .Where(n => n.Recipient == email)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync());

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var notification = await db.Notifications.FindAsync(id);
        if (notification is null)
        {
            return NotFound();
        }

        notification.IsRead = true;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
