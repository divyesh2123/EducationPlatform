using System.ComponentModel.DataAnnotations;

namespace StudentService.API.DTOs;

public record StudentDto(int Id, string FirstName, string LastName, string Email, DateOnly DateOfBirth, string? PhoneNumber, DateTime CreatedAt);

public record CreateStudentRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress, MaxLength(200)] string Email,
    DateOnly DateOfBirth,
    [MaxLength(20)] string? PhoneNumber);

public record UpdateStudentRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress, MaxLength(200)] string Email,
    DateOnly DateOfBirth,
    [MaxLength(20)] string? PhoneNumber);
