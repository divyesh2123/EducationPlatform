using EventBus.Events;
using EventBus.Extensions;
using Microsoft.EntityFrameworkCore;
using NotificationService.API.Data;
using NotificationService.API.IntegrationEvents;
using NotificationService.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("NotificationDb")));

builder.Services.AddScoped<INotificationSender, NotificationSender>();

// Notification service only consumes events.
builder.Services.AddRabbitMQEventBus(builder.Configuration)
    .AddSubscription<StudentCreatedIntegrationEvent, StudentCreatedNotificationHandler>()
    .AddSubscription<StudentEnrolledIntegrationEvent, StudentEnrolledNotificationHandler>()
    .AddSubscription<EnrollmentCancelledIntegrationEvent, EnrollmentCancelledNotificationHandler>()
    .AddSubscription<CourseCreatedIntegrationEvent, CourseCreatedNotificationHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<NotificationDbContext>().Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
