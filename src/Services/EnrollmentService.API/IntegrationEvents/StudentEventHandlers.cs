using EnrollmentService.API.Data;
using EnrollmentService.API.Models;
using EventBus.Abstractions;
using EventBus.Events;
using Microsoft.EntityFrameworkCore;

namespace EnrollmentService.API.IntegrationEvents;

public class StudentCreatedIntegrationEventHandler(EnrollmentDbContext db, ILogger<StudentCreatedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<StudentCreatedIntegrationEvent>
{
    public async Task Handle(StudentCreatedIntegrationEvent @event)
    {
        // Upsert keeps the handler idempotent if RabbitMQ delivers the same message twice.
        var student = await db.Students.FindAsync(@event.StudentId);
        if (student is null)
        {
            student = new StudentReadModel { StudentId = @event.StudentId };
            db.Students.Add(student);
        }

        student.FullName = $"{@event.FirstName} {@event.LastName}";
        student.Email = @event.Email;
        student.IsActive = true;
        student.LastSyncedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        logger.LogInformation("Student {StudentId} synced into Enrollment service", @event.StudentId);
    }
}

public class StudentUpdatedIntegrationEventHandler(EnrollmentDbContext db, ILogger<StudentUpdatedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<StudentUpdatedIntegrationEvent>
{
    public async Task Handle(StudentUpdatedIntegrationEvent @event)
    {
        var student = await db.Students.FindAsync(@event.StudentId);
        if (student is null)
        {
            student = new StudentReadModel { StudentId = @event.StudentId };
            db.Students.Add(student);
        }

        student.FullName = $"{@event.FirstName} {@event.LastName}";
        student.Email = @event.Email;
        student.LastSyncedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        logger.LogInformation("Student {StudentId} updated in Enrollment service", @event.StudentId);
    }
}

/// <summary>
/// Choreography example: a deleted student cancels their enrollments, which in turn
/// publishes EnrollmentCancelled events that Notification service reacts to.
/// </summary>
public class StudentDeletedIntegrationEventHandler(EnrollmentDbContext db, IEventBus eventBus, ILogger<StudentDeletedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<StudentDeletedIntegrationEvent>
{
    public async Task Handle(StudentDeletedIntegrationEvent @event)
    {
        var student = await db.Students.FindAsync(@event.StudentId);
        if (student is null)
        {
            return;
        }

        student.IsActive = false;
        student.LastSyncedAt = DateTime.UtcNow;

        var enrollments = await db.Enrollments
            .Include(e => e.Course)
            .Where(e => e.StudentId == @event.StudentId && e.Status == EnrollmentStatus.Active)
            .ToListAsync();

        const string reason = "Student account was removed";
        enrollments.ForEach(e => e.Cancel(reason));
        await db.SaveChangesAsync();

        foreach (var e in enrollments)
        {
            eventBus.Publish(new EnrollmentCancelledIntegrationEvent(e.Id, student.StudentId, student.FullName, student.Email, e.CourseId, e.Course.Title, reason));
        }

        logger.LogInformation("Student {StudentId} deactivated, {Count} enrollment(s) cancelled", @event.StudentId, enrollments.Count);
    }
}
