using Microsoft.EntityFrameworkCore;
using StudentService.API.Models;

namespace StudentService.API.Data;

public class StudentDbContext(DbContextOptions<StudentDbContext> options) : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(s => s.LastName).HasMaxLength(100).IsRequired();
            entity.Property(s => s.Email).HasMaxLength(200).IsRequired();
            entity.Property(s => s.PhoneNumber).HasMaxLength(20);
            entity.HasIndex(s => s.Email).IsUnique();
        });
    }
}
