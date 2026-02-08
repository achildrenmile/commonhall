# CommonHall API - Production Dockerfile
# Multi-stage build for .NET 10 application

# =============================================================================
# Stage 1: Build
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy entire api folder at once
COPY apps/api/ ./

# Restore and build
RUN dotnet restore CommonHall.Api/CommonHall.Api.csproj && \
    dotnet publish CommonHall.Api/CommonHall.Api.csproj \
    -c Release \
    -o /app/publish

# =============================================================================
# Stage 2: Runtime
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r commonhall && useradd -r -g commonhall commonhall

# Copy published application
COPY --from=build /app/publish .

# Create directories for file storage and logs
RUN mkdir -p /app/storage /app/logs \
    && chown -R commonhall:commonhall /app

# Switch to non-root user
USER commonhall

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "CommonHall.Api.dll"]
