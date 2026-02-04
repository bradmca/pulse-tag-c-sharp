using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using PulseTag.Api.Services;
using PulseTag.Shared.Models;

namespace PulseTag.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowFrontend")]
public class AnalyzeController : ControllerBase
{
    private readonly ISocialScraper _scraper;
    private readonly IAIEngine _aiEngine;
    private readonly ILogger<AnalyzeController> _logger;

    public AnalyzeController(
        ISocialScraper scraper,
        IAIEngine aiEngine,
        ILogger<AnalyzeController> logger)
    {
        _scraper = scraper;
        _aiEngine = aiEngine;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<AnalyzeResponse>> Analyze([FromBody] AnalyzeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate URL to prevent SSRF
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri))
            {
                return BadRequest(new { detail = "Invalid URL format" });
            }

            // Extract text from the social media post
            var textContent = await _scraper.ExtractTextAsync(request.Url, cancellationToken);
            
            if (string.IsNullOrEmpty(textContent))
            {
                // Provide more specific error message
                if (request.Url.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new 
                    { 
                        detail = "Could not extract text from LinkedIn. LinkedIn may require authentication. Please try with a public post or a different platform." 
                    });
                }
                else if (request.Url.Contains("twitter.com", StringComparison.OrdinalIgnoreCase) || 
                         request.Url.Contains("x.com", StringComparison.OrdinalIgnoreCase))
                {
                    // Check if it's a profile URL
                    if (!request.Url.Contains("/status/", StringComparison.OrdinalIgnoreCase))
                    {
                        return BadRequest(new 
                        { 
                            detail = "This appears to be a Twitter profile URL. Please provide a URL to a specific tweet. Click on the tweet and copy that URL instead." 
                        });
                    }
                    else
                    {
                        return BadRequest(new 
                        { 
                            detail = "Could not extract text from this X/Tweet. Please ensure the tweet is public and accessible." 
                        });
                    }
                }
                else
                {
                    return BadRequest(new 
                    { 
                        detail = "Could not extract text from the provided URL. Please ensure it's a valid and accessible web page." 
                    });
                }
            }
            
            // Generate hashtags using AI
            var hashtags = await _aiEngine.AnalyzePostAsync(textContent, cancellationToken);
            
            return Ok(new AnalyzeResponse
            {
                OriginalText = textContent,
                Hashtags = hashtags
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid URL provided: {Url}", request.Url);
            return BadRequest(new { detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error analyzing post from {Url}", request.Url);
            return StatusCode(500, new { detail = "An unexpected error occurred. Please try again later." });
        }
    }
}
