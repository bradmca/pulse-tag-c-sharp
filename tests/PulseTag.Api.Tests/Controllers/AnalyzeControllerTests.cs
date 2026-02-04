using Xunit;
using PulseTag.Api.Controllers;
using PulseTag.Api.Services;
using PulseTag.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace PulseTag.Api.Tests.Controllers;

public class AnalyzeControllerTests
{
    private readonly Mock<ISocialScraper> _mockScraper;
    private readonly Mock<IAIEngine> _mockAiEngine;
    private readonly Mock<ILogger<AnalyzeController>> _mockLogger;
    private readonly AnalyzeController _controller;

    public AnalyzeControllerTests()
    {
        _mockScraper = new Mock<ISocialScraper>();
        _mockAiEngine = new Mock<IAIEngine>();
        _mockLogger = new Mock<ILogger<AnalyzeController>>();
        _controller = new AnalyzeController(_mockScraper.Object, _mockAiEngine.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Analyze_WithValidUrl_ReturnsSuccess()
    {
        // Arrange
        var request = new AnalyzeRequest { Url = "https://linkedin.com/posts/example" };
        var expectedText = "Sample post text";
        var expectedHashtags = new HashtagDictionary
        {
            Safe = new List<string> { "Marketing", "Business" },
            Rising = new List<string> { "GrowthHacking" },
            Niche = new List<string> { "B2BMarketing" }
        };

        _mockScraper.Setup(x => x.ExtractTextAsync(request.Url, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedText);
        _mockAiEngine.Setup(x => x.AnalyzePostAsync(expectedText, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedHashtags);

        // Act
        var result = await _controller.Analyze(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AnalyzeResponse>(okResult.Value);
        Assert.Equal(expectedText, response.OriginalText);
        Assert.Equal(expectedHashtags.Safe, response.Hashtags.Safe);
        Assert.Equal(expectedHashtags.Rising, response.Hashtags.Rising);
        Assert.Equal(expectedHashtags.Niche, response.Hashtags.Niche);
    }

    [Fact]
    public async Task Analyze_WithInvalidUrl_ReturnsBadRequest()
    {
        // Arrange
        var request = new AnalyzeRequest { Url = "invalid-url" };

        // Act
        var result = await _controller.Analyze(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Analyze_WithUnsupportedDomain_ReturnsBadRequest()
    {
        // Arrange
        var request = new AnalyzeRequest { Url = "https://example.com/post" };

        _mockScraper.Setup(x => x.ExtractTextAsync(request.Url, It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new ArgumentException("URL domain not allowed"));

        // Act
        var result = await _controller.Analyze(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }
}
