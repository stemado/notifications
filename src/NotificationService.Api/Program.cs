using NotificationService.Api.Extensions;
using NotificationService.Api.Hubs;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting.WindowsServices;
using System.Text.Json;

// Configure for Windows Service support
var options = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService()
        ? AppContext.BaseDirectory
        : default
};

var builder = WebApplication.CreateBuilder(options);

// Configure URLs
const string fullUrlPath = "http://anf-srv06.antfarmllc.local:5201";
const string localUrlPath = "http://localhost:5201";

builder.WebHost.UseUrls(fullUrlPath, localUrlPath);

// Configure Windows Service support
builder.Host.UseWindowsService();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add notification services
builder.Services.AddNotifications(builder.Configuration, builder.Environment);

// Add JWT authentication (Phase 3)
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add Health Checks
// Use the same environment variable as the main database connection for consistency
var healthCheckConnectionString = Environment.GetEnvironmentVariable("NOTIFICATIONS_CONNECTION_STRING", EnvironmentVariableTarget.Machine)
    ?? throw new InvalidOperationException("NOTIFICATIONS_CONNECTION_STRING environment variable is not set.");
builder.Services.AddHealthChecks()
    .AddNpgSql(
        healthCheckConnectionString,
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "postgres" })
    .AddCheck("self", () => HealthCheckResult.Healthy("Service is running"), tags: new[] { "self" });

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000", "https://localhost:3000",
            "http://localhost:3001", "https://localhost:3001"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials(); // Required for SignalR
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

// Don't redirect to HTTPS in development
// app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});

// Map SignalR hub
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
