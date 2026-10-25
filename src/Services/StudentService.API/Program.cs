using EventBus.Extensions;
using Microsoft.EntityFrameworkCore;
using StudentService.API.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<StudentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("StudentDb")));

// Student service only publishes events, so no subscriptions are registered.
builder.Services.AddRabbitMQEventBus(builder.Configuration);

var app = builder.Build();

// Code-first: create/upgrade the database from the EF Core migrations on start-up.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<StudentDbContext>().Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
