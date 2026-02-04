using Microsoft.AspNetCore.Mvc;
using PulseTag.Shared.Models;

namespace PulseTag.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IConfiguration configuration, ILogger<HealthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<HealthResponse> Get()
    {
        try
        {
            var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0";
            
            return Ok(new HealthResponse
            {
                Status = "healthy",
                Version = version,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new HealthResponse 
            { 
                Status = "unhealthy",
                Version = "unknown",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
