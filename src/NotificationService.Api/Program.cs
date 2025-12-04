using NotificationService.Api.Extensions;
using NotificationService.Api.Hubs;
using NotificationService.Api.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting.WindowsServices;
using System.Text.Json;
using System.Text.Json.Serialization;

// Enable Npgsql legacy timestamp behavior to handle DateTime with unspecified Kind
// This must be called before any Npgsql connection is made
// See: https://www.npgsql.org/doc/types/datetime.html
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

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
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enable string-based enum serialization for API requests/responses
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add notification services
builder.Services.AddNotifications(builder.Configuration, builder.Environment);

// Add JWT authentication (Phase 3) - Only in Production
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddJwtAuthentication(builder.Configuration);
}

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
            "http://localhost:3001", "https://localhost:3001",
            "http://192.168.150.52:3000", "https://192.168.150.52:3000",
            "http://anf-srv06.antfarmllc.local:3000", "https://anf-srv06.antfarmllc.local:3000"
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

// In development, use middleware to inject a default system user
// This allows the API to work without Keycloak
// This middleware is safe to use always - it only injects a user if not authenticated
app.UseMiddleware<DevelopmentUserMiddleware>();

// Only use authentication in Production
if (!app.Environment.IsDevelopment())
{
    app.UseAuthentication();
    app.UseAuthorization();
}

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
