namespace PulseTag.Shared.Models;

public class AnalyzeRequest
{
    public string Url { get; set; } = string.Empty;
}

public class AnalyzeResponse
{
    public string OriginalText { get; set; } = string.Empty;
    public HashtagDictionary Hashtags { get; set; } = new();
}

public class HashtagDictionary
{
    public List<string> Safe { get; set; } = new();
    public List<string> Rising { get; set; } = new();
    public List<string> Niche { get; set; } = new();
}

public class HealthResponse
{
    public string Status { get; set; } = "healthy";
    public string Version { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
