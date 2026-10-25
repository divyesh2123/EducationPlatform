using EnrollmentService.API.Data;
using EnrollmentService.API.IntegrationEvents;
using EventBus.Events;
using EventBus.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<EnrollmentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("EnrollmentDb")));

// Enrollment service both publishes (StudentEnrolled, EnrollmentCancelled) and consumes events.
builder.Services.AddRabbitMQEventBus(builder.Configuration)
    .AddSubscription<StudentCreatedIntegrationEvent, StudentCreatedIntegrationEventHandler>()
    .AddSubscription<StudentUpdatedIntegrationEvent, StudentUpdatedIntegrationEventHandler>()
    .AddSubscription<StudentDeletedIntegrationEvent, StudentDeletedIntegrationEventHandler>()
    .AddSubscription<CourseCreatedIntegrationEvent, CourseCreatedIntegrationEventHandler>()
    .AddSubscription<CourseUpdatedIntegrationEvent, CourseUpdatedIntegrationEventHandler>()
    .AddSubscription<CourseDeletedIntegrationEvent, CourseDeletedIntegrationEventHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<EnrollmentDbContext>().Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
