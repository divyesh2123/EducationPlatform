using CourseService.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseService.API.Data;

public class CourseDbContext(DbContextOptions<CourseDbContext> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Courses");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Code).HasMaxLength(20).IsRequired();
            entity.Property(c => c.Title).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Description).HasMaxLength(2000);
            entity.Property(c => c.Instructor).HasMaxLength(150).IsRequired();
            entity.HasIndex(c => c.Code).IsUnique();
        });
    }
}
