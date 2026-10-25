namespace EnrollmentService.API.Models;

/// <summary>
/// Local copy of the data this service needs about a course, kept in sync from Course service events.
/// </summary>
public class CourseReadModel
{
    public int CourseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
}
