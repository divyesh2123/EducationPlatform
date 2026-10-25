using EventBus.Abstractions;
using EventBus.Events;
using NotificationService.API.Services;

namespace NotificationService.API.IntegrationEvents;

public class StudentCreatedNotificationHandler(INotificationSender sender) : IIntegrationEventHandler<StudentCreatedIntegrationEvent>
{
    public Task Handle(StudentCreatedIntegrationEvent @event) =>
        sender.SendAsync(@event, @event.Email,
            "Welcome to the Education Platform",
            $"Hi {@event.FirstName}, your student account (ID {@event.StudentId}) has been created. You can now enroll in courses.");
}

public class StudentEnrolledNotificationHandler(INotificationSender sender) : IIntegrationEventHandler<StudentEnrolledIntegrationEvent>
{
    public Task Handle(StudentEnrolledIntegrationEvent @event) =>
        sender.SendAsync(@event, @event.StudentEmail,
            $"Enrollment confirmed: {@event.CourseCode}",
            $"Hi {@event.StudentName}, you are enrolled in {@event.CourseCode} - {@event.CourseTitle} (enrollment #{@event.EnrollmentId}) on {@event.EnrolledAt:yyyy-MM-dd}.");
}

public class EnrollmentCancelledNotificationHandler(INotificationSender sender) : IIntegrationEventHandler<EnrollmentCancelledIntegrationEvent>
{
    public Task Handle(EnrollmentCancelledIntegrationEvent @event) =>
        sender.SendAsync(@event, @event.StudentEmail,
            $"Enrollment cancelled: {@event.CourseTitle}",
            $"Hi {@event.StudentName}, your enrollment #{@event.EnrollmentId} in {@event.CourseTitle} was cancelled. Reason: {@event.Reason}");
}

public class CourseCreatedNotificationHandler(INotificationSender sender) : IIntegrationEventHandler<CourseCreatedIntegrationEvent>
{
    public Task Handle(CourseCreatedIntegrationEvent @event) =>
        sender.SendAsync(@event, "all-students@education.local",
            $"New course available: {@event.Code}",
            $"{@event.Title} by {@event.Instructor} ({@event.Credits} credits, {@event.Capacity} seats) is now open for enrollment.");
}
