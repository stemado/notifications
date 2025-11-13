using NotificationService.Api.Extensions;
using NotificationService.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use port 5200
// NOTE: Changed from 5100 to 5200 to avoid conflicts with existing services (e.g., 5001/5100)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5200);
});


// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add notification services
builder.Services.AddNotifications(builder.Configuration, builder.Environment);

// Add JWT authentication (Phase 3)
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000") // Update with your frontend URL
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

// Map SignalR hub
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
