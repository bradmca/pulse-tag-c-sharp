# Contributing to PulseTag

Thank you for your interest in contributing to PulseTag! This document provides guidelines and information for contributors.

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- Docker and Docker Compose (optional)
- OpenRouter API key (free at https://openrouter.ai/keys)

### Setting Up Your Development Environment

1. Fork the repository
2. Clone your fork:
   ```bash
   git clone https://github.com/YOUR_USERNAME/pulse-tag-c-sharp.git
   cd pulse-tag
   ```

3. Set up the development environment:
   ```bash
   # Copy environment file
   cp .env.example .env
   
   # Add your OpenRouter API key to .env
   # Create development settings
   cp src/PulseTag.Api/appsettings.json src/PulseTag.Api/appsettings.Development.json
   # Add your API key to appsettings.Development.json
   ```

4. Run the application locally:
   ```bash
   # In one terminal - run the API
   dotnet run --project src/PulseTag.Api
   
   # In another terminal - run the Web app
   dotnet run --project src/PulseTag.Web
   ```

5. Access the applications:
   - Frontend: https://localhost:5001
   - API: https://localhost:8000
   - API Documentation: https://localhost:8000/swagger

## Project Structure

```
PulseTag/
├── src/
│   ├── PulseTag.Api/          # ASP.NET Core Web API
│   │   ├── Controllers/       # API controllers
│   │   ├── Services/          # Business logic services
│   │   └── Program.cs         # Application entry point
│   ├── PulseTag.Web/          # Blazor Web App
│   │   ├── Components/        # Blazor components
│   │   └── Program.cs         # Application entry point
│   └── PulseTag.Shared/       # Shared models and DTOs
├── tests/
│   └── PulseTag.Api.Tests/    # Unit tests
└── PulseTag.sln               # Solution file
```

## Development Workflow

### Making Changes

1. Create a new branch for your feature:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. Make your changes following the coding standards below.

3. Test your changes:
   ```bash
   # Run all tests
   dotnet test
   
   # Run tests with coverage
   dotnet test --collect:"XPlat Code Coverage"
   ```

4. Build the solution:
   ```bash
   dotnet build PulseTag.sln --configuration Release
   ```

### Coding Standards

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods small and focused
- Write unit tests for new features

### Testing

- Write unit tests for all new features
- Aim for high test coverage
- Use descriptive test names
- Mock external dependencies using Moq

## Submitting Changes

1. Commit your changes:
   ```bash
   git add .
   git commit -m "feat: add your feature description"
   ```

2. Push to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```

3. Create a Pull Request:
   - Provide a clear description of your changes
   - Reference any related issues
   - Include screenshots if applicable

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests in specific project
dotnet test tests/PulseTag.Api.Tests

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Docker Development

To run the application with Docker:

```bash
# Build and run with Docker Compose
docker-compose up --build

# Run in detached mode
docker-compose up -d

# Stop containers
docker-compose down
```

## Bug Reports

When reporting bugs, please include:

- Clear description of the issue
- Steps to reproduce
- Expected vs actual behavior
- Environment details (.NET version, OS, etc.)
- Relevant logs or screenshots

## Feature Requests

Feature requests are welcome! Please:

- Check if the feature already exists
- Provide a clear use case
- Explain why the feature would be valuable
- Consider if you can contribute the implementation

## Code Review Process

- All submissions require review
- Maintain polite and constructive feedback
- Address review comments promptly
- Request re-review after making changes

## Community Guidelines

- Be respectful and inclusive
- Help others learn and grow
- Focus on constructive feedback
- Follow the [Code of Conduct](CODE_OF_CONDUCT.md)

## Getting Help

- Check existing issues and discussions
- Read the documentation
- Ask questions in discussions
- Contact maintainers if needed

Thank you for contributing to PulseTag! 🚀
