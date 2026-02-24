using Microsoft.EntityFrameworkCore;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Data;
using Nexus.Infrastructure.Repositories;

namespace Nexus.Tests;

/// <summary>
/// Smart Foldering testləri - Ağac strukturunun avtomatik yaradılması
/// </summary>
public class SmartFolderingTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly DocumentRepository _repository;

    public SmartFolderingTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new DocumentRepository(_context);
    }

    [Fact]
    public async Task GetOrCreatePathAsync_CreateNewHierarchy_ReturnsDocument()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            IdareCode = "AZNEFT_IB",
            IdareName = "Azneft İB",
            QuyuCode = "QUYU_020",
            QuyuName = "20 saylı quyu",
            MenteqeCode = "MNT_001",
            MenteqeName = "1 nömrəli məntəqə",
            DocumentDate = new DateTime(2026, 2, 24),
            DocumentNumber = "M-0456",
            Subject = "Qazma işləri",
            OpenTextId = "OT_12345"
        };

        // Act
        var document = await _repository.CreateDocumentWithPathAsync(request);

        // Assert
        Assert.NotNull(document);
        Assert.Equal(NodeType.DOCUMENT, document.NodeType);
        Assert.Contains("2026-02-24", document.EntityName);
        Assert.Contains("M-0456", document.EntityName);
        Assert.Contains("Qazma işləri", document.EntityName);

        // Yoxla: Idare, Quyu, Menteqe yaradıldımı
        var idare = await _context.DocumentNodes
            .FirstOrDefaultAsync(x => x.EntityCode == "AZNEFT_IB");
        Assert.NotNull(idare);
        Assert.Equal("Azneft İB", idare.EntityName);

        var quyu = await _context.DocumentNodes
            .FirstOrDefaultAsync(x => x.EntityCode == "QUYU_020");
        Assert.NotNull(quyu);
        Assert.Equal(quyu.ParentNodeId, idare.NodeId);

        var menteqe = await _context.DocumentNodes
            .FirstOrDefaultAsync(x => x.EntityCode == "MNT_001");
        Assert.NotNull(menteqe);
        Assert.Equal(menteqe.ParentNodeId, quyu.NodeId);
    }

    [Fact]
    public async Task GetOrCreatePathAsync_DuplicateIdare_ReusesExisting()
    {
        // Arrange - Birinci sənəd
        var request1 = new CreateDocumentRequest
        {
            IdareCode = "AZNEFT_IB",
            IdareName = "Azneft İB",
            QuyuCode = "QUYU_020",
            QuyuName = "20 saylı quyu",
            MenteqeCode = "MNT_001",
            MenteqeName = "1 nömrəli məntəqə",
            DocumentDate = new DateTime(2026, 2, 24),
            DocumentNumber = "M-0456",
            Subject = "İlk sənəd",
            OpenTextId = "OT_1"
        };
        await _repository.CreateDocumentWithPathAsync(request1);

        // İkinci sənəd - eyni Idare, yeni Quyu
        var request2 = new CreateDocumentRequest
        {
            IdareCode = "AZNEFT_IB",
            IdareName = "Azneft İB (update olunmamalı)",
            QuyuCode = "QUYU_030",
            QuyuName = "30 saylı quyu",
            MenteqeCode = "MNT_001",
            MenteqeName = "Məntəqə A",
            DocumentDate = new DateTime(2026, 2, 25),
            DocumentNumber = "M-0457",
            Subject = "İkinci sənəd",
            OpenTextId = "OT_2"
        };

        // Act
        await _repository.CreateDocumentWithPathAsync(request2);

        // Assert
        var idares = await _context.DocumentNodes
            .Where(x => x.EntityCode == "AZNEFT_IB")
            .ToListAsync();
        
        // Yalnız 1 Idare olmalıdır
        Assert.Single(idares);
        Assert.Equal("Azneft İB", idares[0].EntityName); // İlk ad qalmalı

        // 2 fərqli Quyu olmalıdır
        var quyular = await _context.DocumentNodes
            .Where(x => x.NodeType == NodeType.QUYU)
            .ToListAsync();
        Assert.Equal(2, quyular.Count);
    }

    [Fact]
    public async Task GetOrCreatePathAsync_ExistingPath_ReusesAll()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            IdareCode = "AZNEFT_IB",
            IdareName = "Azneft İB",
            QuyuCode = "QUYU_020",
            QuyuName = "20 saylı quyu",
            MenteqeCode = "MNT_001",
            MenteqeName = "1 nömrəli məntəqə",
            DocumentDate = new DateTime(2026, 2, 24),
            DocumentNumber = "M-0456",
            Subject = "Test",
            OpenTextId = "OT_1"
        };
        
        // Birinci sənəd - bütün ağacı yaradır
        await _repository.CreateDocumentWithPathAsync(request);

        // İkinci sənəd - eyni yola
        request.DocumentNumber = "M-0457";
        request.Subject = "Yeni sənəd";
        request.OpenTextId = "OT_2";

        // Act
        await _repository.CreateDocumentWithPathAsync(request);

        // Assert - Hər node tipindən yalnız 1 olmalıdır
        Assert.Equal(1, await _context.DocumentNodes.CountAsync(x => x.NodeType == NodeType.IDARE));
        Assert.Equal(1, await _context.DocumentNodes.CountAsync(x => x.NodeType == NodeType.QUYU));
        Assert.Equal(1, await _context.DocumentNodes.CountAsync(x => x.NodeType == NodeType.MENTEQE));
        Assert.Equal(2, await _context.DocumentNodes.CountAsync(x => x.NodeType == NodeType.DOCUMENT));
    }

    [Fact]
    public async Task CreateDocument_FileNameFormat_IsCorrect()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            IdareCode = "AZNEFT_IB",
            IdareName = "Azneft İB",
            QuyuCode = "QUYU_020",
            QuyuName = "20 saylı quyu",
            MenteqeCode = "MNT_001",
            MenteqeName = "1 nömrəli məntəqə",
            DocumentDate = new DateTime(2026, 2, 24),
            DocumentNumber = "M-0456",
            Subject = "Qazma işlərinin təhvil-təslimi",
            OpenTextId = "OT_12345"
        };

        // Act
        var document = await _repository.CreateDocumentWithPathAsync(request);

        // Assert
        Assert.Equal("2026-02-24 - Məktub №M-0456 - Qazma işlərinin təhvil-təslimi.pdf", document.EntityName);
    }

    [Fact]
    public async Task SearchDocuments_ByIdareCode_ReturnsResults()
    {
        // Arrange
        await SeedTestData();

        // Act
        var results = await _repository.SearchDocumentsAsync(new SearchRequest
        {
            IdareCode = "AZNEFT_IB"
        });

        // Assert
        Assert.All(results, doc => 
            Assert.StartsWith("/1/", doc.MaterializedPath));
    }

    private async Task SeedTestData()
    {
        var requests = new[]
        {
            new CreateDocumentRequest
            {
                IdareCode = "AZNEFT_IB",
                IdareName = "Azneft İB",
                QuyuCode = "QUYU_020",
                QuyuName = "20 saylı quyu",
                MenteqeCode = "MNT_001",
                MenteqeName = "1 nömrəli məntəqə",
                DocumentDate = new DateTime(2026, 2, 24),
                DocumentNumber = "M-0456",
                Subject = "Test 1",
                OpenTextId = "OT_1"
            },
            new CreateDocumentRequest
            {
                IdareCode = "AZNEFT_IB",
                IdareName = "Azneft İB",
                QuyuCode = "QUYU_020",
                QuyuName = "20 saylı quyu",
                MenteqeCode = "MNT_002",
                MenteqeName = "2 nömrəli məntəqə",
                DocumentDate = new DateTime(2026, 2, 25),
                DocumentNumber = "M-0457",
                Subject = "Test 2",
                OpenTextId = "OT_2"
            }
        };

        foreach (var req in requests)
            await _repository.CreateDocumentWithPathAsync(req);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
