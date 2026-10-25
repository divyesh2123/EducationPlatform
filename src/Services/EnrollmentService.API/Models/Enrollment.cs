namespace EnrollmentService.API.Models;

public enum EnrollmentStatus
{
    Active,
    Cancelled
}

public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    public StudentReadModel Student { get; set; } = null!;
    public CourseReadModel Course { get; set; } = null!;

    public void Cancel(string reason)
    {
        Status = EnrollmentStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
    }
}
