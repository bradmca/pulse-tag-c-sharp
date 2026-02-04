using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using HtmlAgilityPack;
using PulseTag.Shared.Models;
using System.Web;
using System.Text.Json;

namespace PulseTag.Api.Services;

public interface ISocialScraper
{
    Task<string?> ExtractTextAsync(string url, CancellationToken cancellationToken = default);
}

public class SocialScraper : ISocialScraper
{
    private readonly string _userAgent;
    private readonly string? _linkedinCookies;
    private readonly ILogger<SocialScraper> _logger;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, string[]> _linkedinSelectors = new()
    {
        { "article[data-id] .feed-shared-text__text", ["article"] },
        { ".feed-shared-update-v2__description", ["description"] },
        { "[data-test-id=\"main-feed-activity-card\"] .feed-shared-text", ["activity"] },
        { "article .break-words", ["article"] },
        { ".feed-shared-text", ["shared"] }
    };

    private readonly Dictionary<string, string[]> _twitterSelectors = new()
    {
        { "[data-testid=\"tweetText\"]", ["tweet"] },
        { ".tweet-text", ["tweet"] },
        { "[data-test-id=\"tweet\"]", ["test"] },
        { ".css-1dbjc4n.r-1iusvr4.r-16y2uox.r-1kbdv8c", ["css"] }
    };

    public SocialScraper(IConfiguration configuration, ILogger<SocialScraper> logger)
    {
        _userAgent = configuration["UserAgent"] ?? "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        _linkedinCookies = configuration["LinkedIn:Cookies"];
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", _userAgent);
    }

    public async Task<string?> ExtractTextAsync(string url, CancellationToken cancellationToken = default)
    {
        // Validate URL to prevent SSRF - allow all HTTP/HTTPS URLs
        var uri = new Uri(url);
        
        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
            throw new ArgumentException("Only HTTP and HTTPS URLs are supported.");
        }

        // For Twitter/X, try the oEmbed API first (faster and more reliable)
        var isTwitter = url.Contains("twitter.com", StringComparison.OrdinalIgnoreCase) || 
                       url.Contains("x.com", StringComparison.OrdinalIgnoreCase);
        
        if (isTwitter)
        {
            var tweetText = await ExtractTwitterViaOEmbedAsync(url, cancellationToken);
            if (!string.IsNullOrEmpty(tweetText))
            {
                return tweetText;
            }
            _logger.LogWarning("oEmbed API failed, falling back to Playwright for Twitter/X");
        }

        // Fallback to Playwright for browser-based extraction
        return await ExtractTextViaPlaywrightAsync(url, isTwitter, cancellationToken);
    }

    private async Task<string?> ExtractTwitterViaOEmbedAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            // Twitter oEmbed API is publicly accessible
            var oembedUrl = $"https://publish.twitter.com/oembed?url={Uri.EscapeDataString(url)}&omit_script=true";
            _logger.LogInformation("Attempting to fetch tweet via oEmbed API: {Url}", oembedUrl);
            
            var response = await _httpClient.GetAsync(oembedUrl, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("oEmbed API returned status {StatusCode}", response.StatusCode);
                return null;
            }
            
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("html", out var htmlElement))
            {
                var html = htmlElement.GetString();
                if (!string.IsNullOrEmpty(html))
                {
                    // Parse the HTML to extract just the tweet text
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(html);
                    
                    // The tweet text is in the blockquote > p element
                    var pNode = htmlDoc.DocumentNode.SelectSingleNode("//blockquote/p");
                    if (pNode != null)
                    {
                        var text = HttpUtility.HtmlDecode(pNode.InnerText.Trim());
                        _logger.LogInformation("Successfully extracted tweet text via oEmbed: {Length} chars", text.Length);
                        return text;
                    }
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tweet via oEmbed API");
            return null;
        }
    }

    private async Task<string?> ExtractTextViaPlaywrightAsync(string url, bool isTwitter, CancellationToken cancellationToken)
    {
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        
        try
        {
            playwright = await Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            
            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = _userAgent
            });

            // Add LinkedIn cookies if available
            if (url.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(_linkedinCookies))
            {
                await AddCookiesAsync(context, _linkedinCookies, ".linkedin.com");
            }

            var page = await context.NewPageAsync();
            
            // Add random delay to avoid bot detection
            await Task.Delay(Random.Shared.Next(1000, 3000), cancellationToken);
            
            var response = await page.GotoAsync(url, new PageGotoOptions 
            { 
                // Twitter/X needs NetworkIdle to wait for React content to load
                WaitUntil = isTwitter ? WaitUntilState.NetworkIdle : WaitUntilState.DOMContentLoaded,
                Timeout = isTwitter ? 60000 : 30000 
            });

            if (response == null)
            {
                _logger.LogWarning("Failed to navigate to URL: {Url}", url);
                return null;
            }

            // Wait longer for Twitter/X dynamic content to load
            var waitTime = isTwitter ? 5000 : 2000;
            await page.WaitForTimeoutAsync(waitTime);
            
            // For Twitter/X, try to wait for tweet text to appear
            if (isTwitter)
            {
                try
                {
                    await page.WaitForSelectorAsync("[data-testid='tweetText']", new PageWaitForSelectorOptions { Timeout = 10000 });
                }
                catch
                {
                    _logger.LogWarning("Tweet text selector not found, continuing with fallback extraction");
                }
            }

            var html = await page.ContentAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            string? text = null;
            
            if (url.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase))
            {
                text = ExtractLinkedInText(doc);
            }
            else if (isTwitter)
            {
                text = ExtractTwitterText(doc);
            }
            else
            {
                // For non-social media URLs, try to extract general content
                text = ExtractGeneralText(doc);
            }

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from {Url}: {Error}", url, ex.Message);
            return null;
        }
        finally
        {
            if (browser != null)
            {
                await browser.CloseAsync();
            }
            
            playwright?.Dispose();
        }
    }

    private string? ExtractLinkedInText(HtmlDocument doc)
    {
        foreach (var selector in _linkedinSelectors.Keys)
        {
            var node = doc.DocumentNode.SelectSingleNode($"//{selector}");
            if (node != null)
            {
                var text = HttpUtility.HtmlDecode(node.InnerText.Trim());
                if (text.Length > 50)
                {
                    return text;
                }
            }
        }

        // Fallback: look for any article with substantial text
        var articles = doc.DocumentNode.SelectNodes("//article");
        if (articles != null)
        {
            foreach (var article in articles)
            {
                var text = HttpUtility.HtmlDecode(article.InnerText.Trim());
                if (text.Length > 50)
                {
                    return text;
                }
            }
        }

        return null;
    }

    private string? ExtractTwitterText(HtmlDocument doc)
    {
        _logger.LogInformation("Attempting to extract Twitter/X text content");
        
        // Check if it's a profile page
        var profileHeader = doc.DocumentNode.SelectSingleNode("//div[@data-testid='profileHeader']");
        if (profileHeader != null)
        {
            return "This appears to be a Twitter profile page, not a specific tweet. Please provide a URL to an individual tweet.";
        }

        // Primary selector - data-testid='tweetText' is the most reliable for X.com
        var tweetTextNode = doc.DocumentNode.SelectSingleNode("//*[@data-testid='tweetText']");
        if (tweetTextNode != null)
        {
            var text = HttpUtility.HtmlDecode(tweetTextNode.InnerText.Trim());
            _logger.LogInformation("Found tweet text via data-testid='tweetText': {Length} chars", text.Length);
            if (text.Length > 10)
            {
                return text;
            }
        }

        // Try other selectors from our dictionary
        foreach (var selector in _twitterSelectors.Keys)
        {
            var node = doc.DocumentNode.SelectSingleNode($"//{selector}");
            if (node != null)
            {
                var text = HttpUtility.HtmlDecode(node.InnerText.Trim());
                _logger.LogInformation("Found text via selector {Selector}: {Length} chars", selector, text.Length);
                if (text.Length > 20)
                {
                    return text;
                }
            }
        }

        // Fallback: look for div with lang attribute (tweets have language)
        var tweetDivs = doc.DocumentNode.SelectNodes("//div[@lang and @dir='auto']");
        if (tweetDivs != null)
        {
            _logger.LogInformation("Found {Count} div elements with lang attribute", tweetDivs.Count);
            foreach (var div in tweetDivs)
            {
                var text = HttpUtility.HtmlDecode(div.InnerText.Trim());
                if (text.Length > 20)
                {
                    _logger.LogInformation("Found tweet text via lang attribute: {Length} chars", text.Length);
                    return text;
                }
            }
        }

        // Additional fallback: Look for article elements
        var articles = doc.DocumentNode.SelectNodes("//article[@data-testid='tweet']");
        if (articles != null)
        {
            foreach (var article in articles)
            {
                // Find the tweet text within the article
                var textNode = article.SelectSingleNode(".//*[@data-testid='tweetText']") ?? 
                              article.SelectSingleNode(".//div[@lang]");
                if (textNode != null)
                {
                    var text = HttpUtility.HtmlDecode(textNode.InnerText.Trim());
                    if (text.Length > 10)
                    {
                        _logger.LogInformation("Found tweet text in article: {Length} chars", text.Length);
                        return text;
                    }
                }
            }
        }

        _logger.LogWarning("Could not extract Twitter/X text from page");
        return null;
    }

    private string? ExtractGeneralText(HtmlDocument doc)
    {
        var selectors = new[]
        {
            "//main//article",
            "//article",
            "//*[@class='content']",
            "//*[@class='post-content']",
            "//*[@class='entry-content']",
            "//main//p",
            "//body//p"
        };

        foreach (var selector in selectors)
        {
            var nodes = doc.DocumentNode.SelectNodes(selector);
            if (nodes != null)
            {
                var texts = nodes.Select(n => HttpUtility.HtmlDecode(n.InnerText.Trim()))
                               .Where(t => t.Length > 10)
                               .ToList();
                
                if (texts.Any())
                {
                    return string.Join(" ", texts);
                }
            }
        }

        // Fallback: get title and meta description
        var title = doc.DocumentNode.SelectSingleNode("//title");
        var metaDesc = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");

        var parts = new List<string>();
        if (title != null)
        {
            parts.Add(HttpUtility.HtmlDecode(title.InnerText.Trim()));
        }
        
        if (metaDesc != null)
        {
            var content = metaDesc.GetAttributeValue("content", "");
            if (!string.IsNullOrEmpty(content))
            {
                parts.Add(HttpUtility.HtmlDecode(content));
            }
        }

        return parts.Any() ? string.Join(" ", parts) : null;
    }

    private async Task AddCookiesAsync(IBrowserContext context, string cookiesString, string domain)
    {
        try
        {
            var cookies = new List<Cookie>();
            
            foreach (var cookiePair in cookiesString.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = cookiePair.Trim().Split('=', 2);
                if (parts.Length == 2)
                {
                    cookies.Add(new Cookie
                    {
                        Name = parts[0].Trim(),
                        Value = parts[1].Trim(),
                        Domain = domain,
                        Path = "/"
                    });
                }
            }

            if (cookies.Any())
            {
                await context.AddCookiesAsync(cookies);
                _logger.LogInformation("Added {Count} cookies for domain {Domain}", cookies.Count, domain);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding cookies");
        }
    }
}
