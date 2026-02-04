# Use the official .NET 10.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["src/PulseTag.Api/PulseTag.Api.csproj", "src/PulseTag.Api/"]
COPY ["src/PulseTag.Shared/PulseTag.Shared.csproj", "src/PulseTag.Shared/"]
RUN dotnet restore "src/PulseTag.Api/PulseTag.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/PulseTag.Api"
RUN dotnet build "PulseTag.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PulseTag.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user
RUN useradd -m -u 1001 appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "PulseTag.Api.dll"]
