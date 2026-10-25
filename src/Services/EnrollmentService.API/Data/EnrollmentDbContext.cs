using EnrollmentService.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EnrollmentService.API.Data;

public class EnrollmentDbContext(DbContextOptions<EnrollmentDbContext> options) : DbContext(options)
{
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<StudentReadModel> Students => Set<StudentReadModel>();
    public DbSet<CourseReadModel> Courses => Set<CourseReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StudentReadModel>(entity =>
        {
            entity.ToTable("Students");
            entity.HasKey(s => s.StudentId);
            // Id comes from Student service, so it must not be generated here.
            entity.Property(s => s.StudentId).ValueGeneratedNever();
            entity.Property(s => s.FullName).HasMaxLength(200).IsRequired();
            entity.Property(s => s.Email).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<CourseReadModel>(entity =>
        {
            entity.ToTable("Courses");
            entity.HasKey(c => c.CourseId);
            entity.Property(c => c.CourseId).ValueGeneratedNever();
            entity.Property(c => c.Code).HasMaxLength(20).IsRequired();
            entity.Property(c => c.Title).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.ToTable("Enrollments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.CancellationReason).HasMaxLength(500);

            entity.HasOne(e => e.Student).WithMany().HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Course).WithMany().HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.Restrict);

            // A student can have only one *active* enrollment per course.
            entity.HasIndex(e => new { e.StudentId, e.CourseId })
                .IsUnique()
                .HasFilter("[Status] = 'Active'");
        });
    }
}
