namespace EnrollmentService.API.Models;

/// <summary>
/// Local copy of the data this service needs about a student, kept in sync from Student service events.
/// This avoids a synchronous HTTP call to Student service while enrolling (loose coupling).
/// </summary>
public class StudentReadModel
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
}
