using CourseService.API.Data;
using CourseService.API.DTOs;
using CourseService.API.Models;
using EventBus.Abstractions;
using EventBus.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourseService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController(CourseDbContext db, IEventBus eventBus) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetAll()
    {
        var courses = await db.Courses.AsNoTracking().OrderBy(c => c.Code).ToListAsync();
        return Ok(courses.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CourseDto>> GetById(int id)
    {
        var course = await db.Courses.FindAsync(id);
        return course is null ? NotFound() : Ok(ToDto(course));
    }

    [HttpPost]
    public async Task<ActionResult<CourseDto>> Create(CreateCourseRequest request)
    {
        if (await db.Courses.AnyAsync(c => c.Code == request.Code))
        {
            return Conflict($"A course with code '{request.Code}' already exists.");
        }

        var course = new Course
        {
            Code = request.Code,
            Title = request.Title,
            Description = request.Description,
            Instructor = request.Instructor,
            Credits = request.Credits,
            Capacity = request.Capacity,
            StartDate = request.StartDate
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync();

        eventBus.Publish(new CourseCreatedIntegrationEvent(course.Id, course.Code, course.Title, course.Instructor, course.Credits, course.Capacity));

        return CreatedAtAction(nameof(GetById), new { id = course.Id }, ToDto(course));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCourseRequest request)
    {
        var course = await db.Courses.FindAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        course.Title = request.Title;
        course.Description = request.Description;
        course.Instructor = request.Instructor;
        course.Credits = request.Credits;
        course.Capacity = request.Capacity;
        course.StartDate = request.StartDate;
        course.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        eventBus.Publish(new CourseUpdatedIntegrationEvent(course.Id, course.Code, course.Title, course.Instructor, course.Credits, course.Capacity));

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var course = await db.Courses.FindAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        db.Courses.Remove(course);
        await db.SaveChangesAsync();

        eventBus.Publish(new CourseDeletedIntegrationEvent(id));

        return NoContent();
    }

    private static CourseDto ToDto(Course c) =>
        new(c.Id, c.Code, c.Title, c.Description, c.Instructor, c.Credits, c.Capacity, c.StartDate);
}
