# Use the official .NET 10.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["src/PulseTag.Web/PulseTag.Web.csproj", "src/PulseTag.Web/"]
COPY ["src/PulseTag.Shared/PulseTag.Shared.csproj", "src/PulseTag.Shared/"]
RUN dotnet restore "src/PulseTag.Web/PulseTag.Web.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/PulseTag.Web"
RUN dotnet build "PulseTag.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PulseTag.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user and keys directory
RUN useradd -m -u 1001 appuser && mkdir -p /app/keys && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "PulseTag.Web.dll"]
