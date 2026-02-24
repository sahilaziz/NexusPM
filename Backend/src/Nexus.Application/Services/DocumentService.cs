using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;

namespace Nexus.Application.Services;

/// <summary>
/// Smart Foldering - Business Logic
/// </summary>
public interface IDocumentService
{
    Task<DocumentNode> CreateDocumentWithPathAsync(CreateDocumentRequest request);
    Task<IEnumerable<DocumentNode>> GetFolderTreeAsync(long parentNodeId = 1);
    Task<IEnumerable<DocumentNode>> SearchDocumentsAsync(SearchRequest request);
    Task<DocumentNode?> GetDocumentAsync(long nodeId);
    Task<IEnumerable<SyncQueue>> GetPendingSyncItemsAsync(string organizationCode);
    Task MarkSyncAsCompletedAsync(long syncQueueId);
    Task<DocumentNode> UpdateExternalNumberAsync(long nodeId, string externalNumber);
}

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _repository;
    private readonly IDocumentIdentifierService _identifierService;

    public DocumentService(
        IDocumentRepository repository,
        IDocumentIdentifierService identifierService)
    {
        _repository = repository;
        _identifierService = identifierService;
    }

    public async Task<DocumentNode> CreateDocumentWithPathAsync(CreateDocumentRequest request)
    {
        // 1. Sənəd identifikatoru yarat (daxili və ya xarici)
        var identifierResult = await _identifierService.CreateIdentifierAsync(
            request.SourceType,
            request.ExternalDocumentNumber,
            request.IdareCode);

        // 2. Path-i tap və ya yarat (Idare → Quyu → Menteqe)
        var menteqe = await _repository.GetOrCreatePathAsync(
            request.IdareCode, request.IdareName,
            request.QuyuCode, request.QuyuName,
            request.MenteqeCode, request.MenteqeName,
            request.CreatedBy);

        // 3. Sənədi yarat
        var document = await _repository.CreateDocumentAsync(
            menteqe.NodeId,
            identifierResult.DocumentNumber,
            request.DocumentSubject,
            request.DocumentDate,
            request.CreatedBy,
            identifierResult.NormalizedNumber,
            request.ExternalDocumentNumber,
            request.SourceType);

        return document;
    }

    public async Task<IEnumerable<DocumentNode>> GetFolderTreeAsync(long parentNodeId = 1)
    {
        return await _repository.GetChildrenAsync(parentNodeId);
    }

    public async Task<IEnumerable<DocumentNode>> SearchDocumentsAsync(SearchRequest request)
    {
        // Əgər sənəd nömrəsi axtarılırsa, smart axtarış et
        if (!string.IsNullOrEmpty(request.SearchTerm) && 
            (request.SearchTerm.Contains('-') || request.SearchTerm.Contains('\\')))
        {
            return await _identifierService.SearchByDocumentNumberAsync(request.SearchTerm);
        }

        // Adi axtarış
        return await _repository.SearchAsync(
            request.SearchTerm,
            request.IdareCode,
            request.DateFrom,
            request.DateTo);
    }

    public async Task<DocumentNode?> GetDocumentAsync(long nodeId)
    {
        return await _repository.GetByIdAsync(nodeId);
    }

    public async Task<IEnumerable<SyncQueue>> GetPendingSyncItemsAsync(string organizationCode)
    {
        return await _repository.GetPendingSyncItemsAsync(organizationCode);
    }

    public async Task MarkSyncAsCompletedAsync(long syncQueueId)
    {
        await _repository.MarkSyncAsCompletedAsync(syncQueueId);
    }

    public async Task<DocumentNode> UpdateExternalNumberAsync(long nodeId, string externalNumber)
    {
        var document = await _repository.GetByIdAsync(nodeId);
        if (document == null)
            throw new InvalidOperationException($"Document {nodeId} not found");
        
        document.ExternalDocumentNumber = externalNumber;
        document.NormalizedDocumentNumber = _identifierService.NormalizeDocumentNumber(externalNumber);
        await _repository.UpdateAsync(document);
        
        return document;
    }
}

// DTOs
public class CreateDocumentRequest
{
    public string IdareCode { get; set; } = null!;
    public string IdareName { get; set; } = null!;
    public string QuyuCode { get; set; } = null!;
    public string QuyuName { get; set; } = null!;
    public string MenteqeCode { get; set; } = null!;
    public string MenteqeName { get; set; } = null!;
    public DateTime DocumentDate { get; set; }
    public string DocumentSubject { get; set; } = null!;
    public string CreatedBy { get; set; } = "system";
    
    // Yeni sənəd identifikatoru parametrləri
    public DocumentSourceType SourceType { get; set; } = DocumentSourceType.IncomingLetter;
    public string? ExternalDocumentNumber { get; set; }  // Daxil olan məktub nömrəsi
}

public class UploadDocumentFormRequest
{
    public string IdareCode { get; set; } = null!;
    public string IdareName { get; set; } = null!;
    public string QuyuCode { get; set; } = null!;
    public string QuyuName { get; set; } = null!;
    public string MenteqeCode { get; set; } = null!;
    public string MenteqeName { get; set; } = null!;
    public DateTime DocumentDate { get; set; }
    public string DocumentNumber { get; set; } = null!;
    public string Subject { get; set; } = null!;
}

public class SearchRequest
{
    public string SearchTerm { get; set; } = null!;
    public string? IdareCode { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
