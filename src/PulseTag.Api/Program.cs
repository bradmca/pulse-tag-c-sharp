using Microsoft.AspNetCore.Cors;
using Microsoft.Playwright;
using PulseTag.Api.Services;
using PulseTag.Shared.Models;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "PulseTag API", 
        Version = "v1",
        Description = "AI-powered hashtag generator API"
    });
});

// CORS configuration
var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(',') ?? new[] { "http://localhost:3000" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register custom services
builder.Services.AddSingleton<ISocialScraper, SocialScraper>();
builder.Services.AddSingleton<IAIEngine, AIEngine>();

// Add caching
builder.Services.AddMemoryCache();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Playwright browser installation
if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    Console.WriteLine("Installing Playwright browsers...");
    var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
    if (exitCode != 0)
    {
        Console.WriteLine($"Playwright browser installation failed with exit code {exitCode}");
    }
    else
    {
        Console.WriteLine("Playwright browsers installed successfully.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PulseTag API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger at root
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/api/health");

// Ensure API routes are available
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
