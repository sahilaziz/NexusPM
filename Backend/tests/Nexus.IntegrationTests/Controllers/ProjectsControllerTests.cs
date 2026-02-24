using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Infrastructure.Data;
using Xunit;

namespace Nexus.IntegrationTests.Controllers;

/// <summary>
/// Projects API Integration Tests
/// </summary>
public class ProjectsControllerTests : IClassFixture<WebApplicationFactory<Nexus.API.Program>>
{
    private readonly WebApplicationFactory<Nexus.API.Program> _factory;
    private readonly HttpClient _client;

    public ProjectsControllerTests(WebApplicationFactory<Nexus.API.Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real DB with InMemory
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetProjects_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/projects");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProjectById_NonExisting_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/projects/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }
}
