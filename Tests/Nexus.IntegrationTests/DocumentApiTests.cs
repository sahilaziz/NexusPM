using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Nexus.Application.Services;
using Nexus.Domain.Entities;
using Xunit;

namespace Nexus.IntegrationTests;

/// <summary>
/// API Integration Tests with Testcontainers (SQL Server)
/// </summary>
public class DocumentApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public DocumentApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(content);
        Assert.Equal("OK", content.status);
    }

    [Fact]
    public async Task CreateDocument_WithValidRequest_ReturnsCreatedDocument()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            IdareCode = "AZNEFT_TEST",
            IdareName = "Test Idare",
            QuyuCode = "QUYU_TEST_001",
            QuyuName = "Test Quyu",
            MenteqeCode = "MNT_TEST_001",
            MenteqeName = "Test Menteqe",
            DocumentDate = new DateTime(2026, 2, 24),
            DocumentNumber = "TEST-001",
            DocumentSubject = "Integration Test Document",
            OpenTextId = "OT_TEST_001",
            CreatedBy = "test_user"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/documents/create-with-path", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var document = await response.Content.ReadFromJsonAsync<DocumentResponse>();
        Assert.NotNull(document);
        Assert.NotNull(document.Data);
        Assert.True(document.Success);
        Assert.Contains("TEST-001", document.Data.EntityName);
    }

    [Fact]
    public async Task GetDocumentTree_ReturnsTreeStructure()
    {
        // Arrange - First create some hierarchy
        await SeedTestHierarchy();

        // Act
        var response = await _client.GetAsync("/api/v1/documents/tree?parentId=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var tree = await response.Content.ReadFromJsonAsync<List<DocumentNode>>();
        Assert.NotNull(tree);
    }

    [Fact]
    public async Task SearchDocuments_WithTerm_ReturnsResults()
    {
        // Arrange
        await SeedTestDocuments();

        // Act
        var response = await _client.GetAsync("/api/v1/documents/search?term=Mehkub");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var results = await response.Content.ReadFromJsonAsync<List<DocumentNode>>();
        Assert.NotNull(results);
    }

    [Fact]
    public async Task CreateDocument_DuplicateIdare_ReusesExisting()
    {
        // Arrange
        var request1 = new CreateDocumentRequest
        {
            IdareCode = "DUP_TEST",
            IdareName = "Original Name",
            QuyuCode = "QUYU_1",
            QuyuName = "Quyu 1",
            MenteqeCode = "MNT_1",
            MenteqeName = "Menteqe 1",
            DocumentDate = DateTime.Now,
            DocumentNumber = "DUP-001",
            DocumentSubject = "First",
            CreatedBy = "test"
        };

        var request2 = new CreateDocumentRequest
        {
            IdareCode = "DUP_TEST",  // Same code
            IdareName = "Different Name",  // Different name (should be ignored)
            QuyuCode = "QUYU_2",  // Different Quyu
            QuyuName = "Quyu 2",
            MenteqeCode = "MNT_2",
            MenteqeName = "Menteqe 2",
            DocumentDate = DateTime.Now,
            DocumentNumber = "DUP-002",
            DocumentSubject = "Second",
            CreatedBy = "test"
        };

        // Act
        await _client.PostAsJsonAsync("/api/v1/documents/create-with-path", request1);
        await _client.PostAsJsonAsync("/api/v1/documents/create-with-path", request2);

        // Assert - Both documents should exist under same Idare
        var tree = await _client.GetAsync("/api/v1/documents/tree?parentId=1");
        var idares = await tree.Content.ReadFromJsonAsync<List<DocumentNode>>();
        
        var testIdare = idares?.FirstOrDefault(i => i.EntityCode == "DUP_TEST");
        Assert.NotNull(testIdare);
        Assert.Equal("Original Name", testIdare.EntityName); // First name preserved
    }

    private async Task SeedTestHierarchy()
    {
        var request = new CreateDocumentRequest
        {
            IdareCode = "SEED_IB",
            IdareName = "Seed Idare",
            QuyuCode = "SEED_QUYU",
            QuyuName = "Seed Quyu",
            MenteqeCode = "SEED_MNT",
            MenteqeName = "Seed Menteqe",
            DocumentDate = DateTime.Now,
            DocumentNumber = "SEED-001",
            DocumentSubject = "Seed Document",
            CreatedBy = "test"
        };

        await _client.PostAsJsonAsync("/api/v1/documents/create-with-path", request);
    }

    private async Task SeedTestDocuments()
    {
        for (int i = 1; i <= 3; i++)
        {
            var request = new CreateDocumentRequest
            {
                IdareCode = $"SEARCH_IB_{i}",
                IdareName = $"Search Idare {i}",
                QuyuCode = $"SEARCH_QUYU_{i}",
                QuyuName = $"Search Quyu {i}",
                MenteqeCode = $"SEARCH_MNT_{i}",
                MenteqeName = $"Search Menteqe {i}",
                DocumentDate = new DateTime(2026, 2, 20 + i),
                DocumentNumber = $"M-000{i}",
                DocumentSubject = $"Test Məktub {i}",
                CreatedBy = "test"
            };

            await _client.PostAsJsonAsync("/api/v1/documents/create-with-path", request);
        }
    }
}

public class HealthResponse
{
    public string status { get; set; } = string.Empty;
    public DateTime timestamp { get; set; }
}

public class DocumentResponse
{
    public bool Success { get; set; }
    public DocumentNode? Data { get; set; }
    public string? Message { get; set; }
}
