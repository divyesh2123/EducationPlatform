namespace CourseService.API.Models;

public class Course
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Instructor { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int Capacity { get; set; }
    public DateOnly StartDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
