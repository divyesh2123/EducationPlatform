using EnrollmentService.API.Data;
using EnrollmentService.API.Models;
using EventBus.Abstractions;
using EventBus.Events;
using Microsoft.EntityFrameworkCore;

namespace EnrollmentService.API.IntegrationEvents;

public class CourseCreatedIntegrationEventHandler(EnrollmentDbContext db, ILogger<CourseCreatedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<CourseCreatedIntegrationEvent>
{
    public async Task Handle(CourseCreatedIntegrationEvent @event)
    {
        var course = await db.Courses.FindAsync(@event.CourseId);
        if (course is null)
        {
            course = new CourseReadModel { CourseId = @event.CourseId };
            db.Courses.Add(course);
        }

        course.Code = @event.Code;
        course.Title = @event.Title;
        course.Capacity = @event.Capacity;
        course.IsActive = true;
        course.LastSyncedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        logger.LogInformation("Course {CourseId} synced into Enrollment service", @event.CourseId);
    }
}

public class CourseUpdatedIntegrationEventHandler(EnrollmentDbContext db, ILogger<CourseUpdatedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<CourseUpdatedIntegrationEvent>
{
    public async Task Handle(CourseUpdatedIntegrationEvent @event)
    {
        var course = await db.Courses.FindAsync(@event.CourseId);
        if (course is null)
        {
            course = new CourseReadModel { CourseId = @event.CourseId };
            db.Courses.Add(course);
        }

        course.Code = @event.Code;
        course.Title = @event.Title;
        course.Capacity = @event.Capacity;
        course.LastSyncedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        logger.LogInformation("Course {CourseId} updated in Enrollment service", @event.CourseId);
    }
}

public class CourseDeletedIntegrationEventHandler(EnrollmentDbContext db, IEventBus eventBus, ILogger<CourseDeletedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<CourseDeletedIntegrationEvent>
{
    public async Task Handle(CourseDeletedIntegrationEvent @event)
    {
        var course = await db.Courses.FindAsync(@event.CourseId);
        if (course is null)
        {
            return;
        }

        course.IsActive = false;
        course.LastSyncedAt = DateTime.UtcNow;

        var enrollments = await db.Enrollments
            .Include(e => e.Student)
            .Where(e => e.CourseId == @event.CourseId && e.Status == EnrollmentStatus.Active)
            .ToListAsync();

        var reason = $"Course {course.Code} was withdrawn";
        enrollments.ForEach(e => e.Cancel(reason));
        await db.SaveChangesAsync();

        foreach (var e in enrollments)
        {
            eventBus.Publish(new EnrollmentCancelledIntegrationEvent(e.Id, e.StudentId, e.Student.FullName, e.Student.Email, course.CourseId, course.Title, reason));
        }

        logger.LogInformation("Course {CourseId} deactivated, {Count} enrollment(s) cancelled", @event.CourseId, enrollments.Count);
    }
}
