using System.ComponentModel.DataAnnotations;

namespace EnrollmentService.API.DTOs;

public record EnrollmentDto(
    int Id,
    int StudentId,
    string StudentName,
    int CourseId,
    string CourseCode,
    string CourseTitle,
    string Status,
    DateTime EnrolledAt,
    DateTime? CancelledAt,
    string? CancellationReason);

public record CreateEnrollmentRequest([Range(1, int.MaxValue)] int StudentId, [Range(1, int.MaxValue)] int CourseId);

public record CancelEnrollmentRequest([Required, MaxLength(500)] string Reason);
