# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY src/Nexus.Domain/Nexus.Domain.csproj Nexus.Domain/
COPY src/Nexus.Application/Nexus.Application.csproj Nexus.Application/
COPY src/Nexus.Infrastructure/Nexus.Infrastructure.csproj Nexus.Infrastructure/
COPY src/Nexus.API/Nexus.API.csproj Nexus.API/

# Restore dependencies
RUN dotnet restore Nexus.API/Nexus.API.csproj

# Copy source code
COPY src/Nexus.Domain/ Nexus.Domain/
COPY src/Nexus.Application/ Nexus.Application/
COPY src/Nexus.Infrastructure/ Nexus.Infrastructure/
COPY src/Nexus.API/ Nexus.API/

# Build and publish
RUN dotnet publish Nexus.API/Nexus.API.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos "" appuser

COPY --from=build /app/publish .

# Set environment
ENV ASPNETCORE_URLS=http://+:5000
ENV DOTNET_RUNNING_IN_CONTAINER=true

USER appuser

EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "Nexus.API.dll"]
