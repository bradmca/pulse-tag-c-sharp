# PulseTag - AI-Driven Hashtag Generator (.NET)

<img width="1232" height="896" alt="Screenshot 2026-02-04 112300" src="https://github.com/user-attachments/assets/219e819b-4728-4a14-a8b9-c4f9471ef7fc" />

A powerful web application built with .NET 10 that analyzes your social media posts in real-time, suggesting the top trending hashtags to maximize post reach and engagement. Perfect for influencers and marketers who want to stay ahead of the trend curve.

## 🚀 Features

- **🤖 AI-Powered Analysis**: Uses OpenRouter AI to analyze post content and generate relevant hashtags
- **📱 Multi-Platform Support**: Works with LinkedIn, Twitter/X, and general web content
- **🔒 Secure**: Implements SSRF protection, input sanitization, and secure API key handling
- **⚡ High Performance**: Built with ASP.NET Core and Blazor for optimal performance
- **🐳 Docker Support**: Fully containerized for easy deployment
- **🧪 Tested**: Comprehensive unit tests with xUnit
- **📊 CI/CD Ready**: GitHub Actions workflow for automated testing and deployment

## 🏗️ Architecture

### Tech Stack

- **Backend**: ASP.NET Core 10 Web API
- **Frontend**: Blazor Web App (Server-Side Rendering)
- **AI Integration**: OpenAI SDK with OpenRouter
- **Web Scraping**: Playwright for .NET
- **Testing**: xUnit with Moq
- **Containerization**: Docker & Docker Compose
- **CI/CD**: GitHub Actions

### Solution Structure

```
PulseTag/
├── src/
│   ├── PulseTag.Api/          # Web API
│   ├── PulseTag.Web/          # Blazor Frontend
│   └── PulseTag.Shared/       # Shared Models
├── tests/
│   └── PulseTag.Api.Tests/    # Unit Tests
├── .github/workflows/         # CI/CD Pipelines
├── docker-compose.yml         # Docker Compose Configuration
└── PulseTag.sln              # Solution File
```

## 🚀 Quick Start

### Prerequisites

- .NET 10.0 SDK
- Docker (optional)
- OpenRouter API Key (free at [openrouter.ai](https://openrouter.ai))

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/bradmca/pulse-tag-c-sharp.git
   cd pulse-tag
   ```

2. **Set up configuration**
   
   Create `src/PulseTag.Api/appsettings.Development.json`:
   ```json
   {
     "OpenRouter": {
       "ApiKey": "your-openrouter-api-key",
       "Model": "microsoft/phi-3-medium-128k-instruct:free"
     },
     "LinkedIn": {
       "Cookies": "your-linkedin-cookies-optional"
     }
   }
   ```

3. **Run the solution**
   ```bash
   # Run both API and Web
   dotnet run --project src/PulseTag.Api
   dotnet run --project src/PulseTag.Web
   ```

4. **Access the applications**

   **For Docker deployment:**
   - Frontend: http://localhost:3000
   - API: http://localhost:8000
   - API Documentation: http://localhost:8000/swagger

   **For local development:**
   - Frontend: https://localhost:5001
   - API: https://localhost:8000
   - API Documentation: https://localhost:8000/swagger

### Docker Deployment

1. **Using Docker Compose**
   ```bash
   # Create .env file with your configuration
   cp .env.example .env
   # Edit .env with your API keys
   
   # Run with Docker Compose
   docker-compose up -d
   ```

2. **Access the applications**
   - Frontend: http://localhost:3000
   - API: http://localhost:8000

## 📖 API Documentation

### Analyze Endpoint

```http
POST /api/analyze
Content-Type: application/json

{
  "url": "https://linkedin.com/posts/example"
}
```

**Response:**
```json
{
  "originalText": "Your post content...",
  "hashtags": {
    "safe": ["Marketing", "Business"],
    "rising": ["GrowthHacking", "GenAI"],
    "niche": ["B2BMarketingTips"]
  }
}
```

### Health Check

```http
GET /api/health
```

## 🔧 Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `OpenRouter__ApiKey` | OpenRouter API key | Required |
| `OpenRouter__Model` | AI model to use | `microsoft/phi-3-medium-128k-instruct:free` |
| `LinkedIn__Cookies` | LinkedIn cookies for authenticated access | Optional |
| `AllowedOrigins` | CORS allowed origins | `http://localhost:3000` |
| `ApiBaseUrl` | API base URL for frontend | `http://localhost:8000` |

### Supported Platforms

- **LinkedIn**: Works with public posts. Use cookies for private posts.
- **Twitter/X**: Requires individual tweet URLs (must contain `/status/`).
- **General Websites**: Can extract content from most web pages.

## 🧪 Testing

Run the test suite:
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 🚀 Deployment

### Production Deployment

1. **Environment Setup**
   ```bash
   # Create production settings
   cp src/PulseTag.Api/appsettings.json src/PulseTag.Api/appsettings.Production.json
   # Update with production values
   ```

2. **Docker Deployment**
   ```bash
   # Build and deploy
   docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
   ```

3. **Kubernetes**
   ```bash
   # Apply Kubernetes manifests
   kubectl apply -f k8s/
   ```

## 🔒 Security Features

- **SSRF Protection**: URL validation to only allow approved domains
- **Input Sanitization**: Prevents prompt injection attacks
- **Secure Headers**: CORS, CSP, and other security headers
- **Rate Limiting**: Built-in rate limiting capabilities
- **No Hardcoded Secrets**: All secrets via environment variables

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request at https://github.com/bradmca/pulse-tag-c-sharp

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [OpenRouter](https://openrouter.ai) for providing free AI model access
- [Playwright](https://playwright.dev) for reliable web scraping
- [Blazor](https://blazor.net) for the modern web framework
- [.NET](https://dotnet.microsoft.com) for the powerful runtime

## 📞 Support

If you have any questions or issues, please [open an issue](https://github.com/bradmca/pulse-tag-c-sharp/issues) on GitHub.
