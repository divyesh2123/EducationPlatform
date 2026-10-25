using EventBus.Abstractions;

namespace EventBus.Events;

public record StudentEnrolledIntegrationEvent(
    int EnrollmentId,
    int StudentId,
    string StudentName,
    string StudentEmail,
    int CourseId,
    string CourseCode,
    string CourseTitle,
    DateTime EnrolledAt) : IntegrationEvent;

public record EnrollmentCancelledIntegrationEvent(
    int EnrollmentId,
    int StudentId,
    string StudentName,
    string StudentEmail,
    int CourseId,
    string CourseTitle,
    string Reason) : IntegrationEvent;
