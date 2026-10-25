using System.ComponentModel.DataAnnotations;

namespace CourseService.API.DTOs;

public record CourseDto(int Id, string Code, string Title, string? Description, string Instructor, int Credits, int Capacity, DateOnly StartDate);

public record CreateCourseRequest(
    [Required, MaxLength(20)] string Code,
    [Required, MaxLength(200)] string Title,
    [MaxLength(2000)] string? Description,
    [Required, MaxLength(150)] string Instructor,
    [Range(1, 10)] int Credits,
    [Range(1, 500)] int Capacity,
    DateOnly StartDate);

public record UpdateCourseRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(2000)] string? Description,
    [Required, MaxLength(150)] string Instructor,
    [Range(1, 10)] int Credits,
    [Range(1, 500)] int Capacity,
    DateOnly StartDate);
