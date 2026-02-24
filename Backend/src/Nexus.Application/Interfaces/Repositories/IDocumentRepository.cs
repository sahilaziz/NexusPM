using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repositories;

/// <summary>
/// Smart Foldering üçün repository (CQRS dəstəyi ilə)
/// </summary>
public interface IDocumentRepository
{
    // Get
    Task<DocumentNode?> GetByIdAsync(long nodeId);
    Task<DocumentNode?> GetByOpenTextIdAsync(string openTextId);
    Task<IEnumerable<DocumentNode>> GetChildrenAsync(long parentNodeId);
    Task<IEnumerable<DocumentNode>> GetTreeAsync(long rootNodeId = 1);
    
    // Search
    Task<IEnumerable<DocumentNode>> SearchAsync(
        string searchTerm, 
        string? idareCode = null,
        DateTime? dateFrom = null, 
        DateTime? dateTo = null);
    
    // Smart Foldering
    Task<DocumentNode> GetOrCreatePathAsync(
        string idareCode, string idareName,
        string quyuCode, string quyuName,
        string menteqeCode, string menteqeName,
        string createdBy = "system");
    
    Task<DocumentNode> CreateDocumentAsync(
        long parentNodeId,
        string documentNumber,
        string documentSubject,
        DateTime documentDate,
        string createdBy = "system",
        string? normalizedNumber = null,
        string? externalNumber = null,
        DocumentSourceType? sourceType = null);
    
    // CRUD
    Task<DocumentNode> AddAsync(DocumentNode node);
    Task UpdateAsync(DocumentNode node);
    Task DeleteAsync(long nodeId);
    
    // CQRS üçün
    IQueryable<DocumentNode> Query();
    Task SaveChangesAsync();
    
    // Hierarchy helpers
    Task<IEnumerable<DocumentNode>> GetAncestorsAsync(long nodeId);
    Task<IEnumerable<DocumentNode>> GetDescendantsAsync(long nodeId);
    Task<int> GetDepthAsync(long nodeId);
    
    // Smart Search
    Task<IEnumerable<DocumentNode>> SearchByNormalizedNumberAsync(string normalizedNumber);
    
    // Sync Queue
    Task<IEnumerable<SyncQueue>> GetPendingSyncItemsAsync(string organizationCode);
    Task MarkSyncAsCompletedAsync(long syncQueueId);
    Task AddToSyncQueueAsync(SyncQueue syncQueue);
}
