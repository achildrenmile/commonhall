# CommonHall API - Production Dockerfile
# Multi-stage build for .NET 10 application

# =============================================================================
# Stage 1: Build
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build

WORKDIR /src

# Copy project files first for better layer caching
COPY apps/api/CommonHall.Api/CommonHall.Api.csproj ./CommonHall.Api/
COPY apps/api/CommonHall.Application/CommonHall.Application.csproj ./CommonHall.Application/
COPY apps/api/CommonHall.Domain/CommonHall.Domain.csproj ./CommonHall.Domain/
COPY apps/api/CommonHall.Infrastructure/CommonHall.Infrastructure.csproj ./CommonHall.Infrastructure/

# Restore dependencies (using Api project which references all others)
RUN dotnet restore CommonHall.Api/CommonHall.Api.csproj

# Copy source code
COPY apps/api/CommonHall.Api/ ./CommonHall.Api/
COPY apps/api/CommonHall.Application/ ./CommonHall.Application/
COPY apps/api/CommonHall.Domain/ ./CommonHall.Domain/
COPY apps/api/CommonHall.Infrastructure/ ./CommonHall.Infrastructure/

# Build and publish (restore first to ensure all dependencies)
RUN dotnet restore CommonHall.Api/CommonHall.Api.csproj && \
    dotnet publish CommonHall.Api/CommonHall.Api.csproj \
    -c Release \
    -o /app/publish

# =============================================================================
# Stage 2: Runtime
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime

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
