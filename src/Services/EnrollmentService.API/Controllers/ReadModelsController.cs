using EnrollmentService.API.Data;
using EnrollmentService.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnrollmentService.API.Controllers;

/// <summary>
/// Shows the student/course data this service has received via RabbitMQ events (useful to watch eventual consistency).
/// </summary>
[ApiController]
[Route("api/enrollments/read-models")]
public class ReadModelsController(EnrollmentDbContext db) : ControllerBase
{
    [HttpGet("students")]
    public async Task<ActionResult<IEnumerable<StudentReadModel>>> GetStudents() =>
        Ok(await db.Students.AsNoTracking().OrderBy(s => s.StudentId).ToListAsync());

    [HttpGet("courses")]
    public async Task<ActionResult<IEnumerable<CourseReadModel>>> GetCourses() =>
        Ok(await db.Courses.AsNoTracking().OrderBy(c => c.CourseId).ToListAsync());
}
