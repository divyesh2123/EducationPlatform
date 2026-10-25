using EnrollmentService.API.Data;
using EnrollmentService.API.DTOs;
using EnrollmentService.API.Models;
using System.Linq.Expressions;
using EventBus.Abstractions;
using EventBus.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnrollmentService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrollmentsController(EnrollmentDbContext db, IEventBus eventBus) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EnrollmentDto>>> GetAll() =>
        Ok(await Query(_ => true).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EnrollmentDto>> GetById(int id)
    {
        var enrollment = await Query(e => e.Id == id).FirstOrDefaultAsync();
        return enrollment is null ? NotFound() : Ok(enrollment);
    }

    [HttpGet("student/{studentId:int}")]
    public async Task<ActionResult<IEnumerable<EnrollmentDto>>> GetByStudent(int studentId) =>
        Ok(await Query(e => e.StudentId == studentId).ToListAsync());

    [HttpGet("course/{courseId:int}")]
    public async Task<ActionResult<IEnumerable<EnrollmentDto>>> GetByCourse(int courseId) =>
        Ok(await Query(e => e.CourseId == courseId).ToListAsync());

    [HttpPost]
    public async Task<ActionResult<EnrollmentDto>> Enroll(CreateEnrollmentRequest request)
    {
        // Validation uses the local read models only - no HTTP call to Student/Course services.
        var student = await db.Students.FindAsync(request.StudentId);
        if (student is not { IsActive: true })
        {
            return BadRequest($"Student {request.StudentId} not found. If it was just created, the event may not be processed yet.");
        }

        var course = await db.Courses.FindAsync(request.CourseId);
        if (course is not { IsActive: true })
        {
            return BadRequest($"Course {request.CourseId} not found. If it was just created, the event may not be processed yet.");
        }

        var activeEnrollments = db.Enrollments.Where(e => e.CourseId == course.CourseId && e.Status == EnrollmentStatus.Active);

        if (await activeEnrollments.AnyAsync(e => e.StudentId == student.StudentId))
        {
            return Conflict($"Student {student.StudentId} is already enrolled in {course.Code}.");
        }

        if (await activeEnrollments.CountAsync() >= course.Capacity)
        {
            return Conflict($"Course {course.Code} is full (capacity {course.Capacity}).");
        }

        var enrollment = new Enrollment { StudentId = student.StudentId, CourseId = course.CourseId };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        eventBus.Publish(new StudentEnrolledIntegrationEvent(
            enrollment.Id, student.StudentId, student.FullName, student.Email,
            course.CourseId, course.Code, course.Title, enrollment.EnrolledAt));

        var dto = await Query(e => e.Id == enrollment.Id).FirstAsync();
        return CreatedAtAction(nameof(GetById), new { id = enrollment.Id }, dto);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancelEnrollmentRequest request)
    {
        var enrollment = await db.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enrollment is null)
        {
            return NotFound();
        }

        if (enrollment.Status == EnrollmentStatus.Cancelled)
        {
            return Conflict("Enrollment is already cancelled.");
        }

        enrollment.Cancel(request.Reason);
        await db.SaveChangesAsync();

        eventBus.Publish(new EnrollmentCancelledIntegrationEvent(
            enrollment.Id, enrollment.StudentId, enrollment.Student.FullName, enrollment.Student.Email,
            enrollment.CourseId, enrollment.Course.Title, request.Reason));

        return NoContent();
    }

    // Filter is applied on the entity before projecting, so EF Core can translate it to SQL.
    private IQueryable<EnrollmentDto> Query(Expression<Func<Enrollment, bool>> filter) =>
        db.Enrollments
            .AsNoTracking()
            .Where(filter)
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => new EnrollmentDto(
                e.Id, e.StudentId, e.Student.FullName, e.CourseId, e.Course.Code, e.Course.Title,
                e.Status.ToString(), e.EnrolledAt, e.CancelledAt, e.CancellationReason));
}
