using EventBus.Abstractions;
using EventBus.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentService.API.Data;
using StudentService.API.DTOs;
using StudentService.API.Models;

namespace StudentService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController(StudentDbContext db, IEventBus eventBus) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudentDto>>> GetAll()
    {
        var students = await db.Students.AsNoTracking().OrderBy(s => s.Id).ToListAsync();
        return Ok(students.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StudentDto>> GetById(int id)
    {
        var student = await db.Students.FindAsync(id);
        return student is null ? NotFound() : Ok(ToDto(student));
    }

    [HttpPost]
    public async Task<ActionResult<StudentDto>> Create(CreateStudentRequest request)
    {
        if (await db.Students.AnyAsync(s => s.Email == request.Email))
        {
            return Conflict($"A student with email '{request.Email}' already exists.");
        }

        var student = new Student
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            DateOfBirth = request.DateOfBirth,
            PhoneNumber = request.PhoneNumber
        };

        db.Students.Add(student);
        await db.SaveChangesAsync();

        // Other services (Enrollment, Notification) react to this event asynchronously.
        eventBus.Publish(new StudentCreatedIntegrationEvent(student.Id, student.FirstName, student.LastName, student.Email));

        return CreatedAtAction(nameof(GetById), new { id = student.Id }, ToDto(student));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateStudentRequest request)
    {
        var student = await db.Students.FindAsync(id);
        if (student is null)
        {
            return NotFound();
        }

        if (await db.Students.AnyAsync(s => s.Email == request.Email && s.Id != id))
        {
            return Conflict($"A student with email '{request.Email}' already exists.");
        }

        student.FirstName = request.FirstName;
        student.LastName = request.LastName;
        student.Email = request.Email;
        student.DateOfBirth = request.DateOfBirth;
        student.PhoneNumber = request.PhoneNumber;
        student.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        eventBus.Publish(new StudentUpdatedIntegrationEvent(student.Id, student.FirstName, student.LastName, student.Email));

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await db.Students.FindAsync(id);
        if (student is null)
        {
            return NotFound();
        }

        db.Students.Remove(student);
        await db.SaveChangesAsync();

        eventBus.Publish(new StudentDeletedIntegrationEvent(id));

        return NoContent();
    }

    private static StudentDto ToDto(Student s) =>
        new(s.Id, s.FirstName, s.LastName, s.Email, s.DateOfBirth, s.PhoneNumber, s.CreatedAt);
}
