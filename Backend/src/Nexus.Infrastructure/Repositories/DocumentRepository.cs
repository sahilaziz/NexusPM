using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Data;

namespace Nexus.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _context;

    public DocumentRepository(AppDbContext context)
    {
        _context = context;
    }

    // Get
    public async Task<DocumentNode?> GetByIdAsync(long nodeId)
    {
        return await _context.DocumentNodes
            .Include(n => n.Ancestors)
            .Include(n => n.Children)
            .FirstOrDefaultAsync(n => n.NodeId == nodeId && !n.IsDeleted);
    }

    public async Task<DocumentNode?> GetByOpenTextIdAsync(string openTextId)
    {
        // OpenText integration removed
        return await _context.DocumentNodes
            .FirstOrDefaultAsync(n => n.EntityCode == openTextId && !n.IsDeleted);
    }

    public async Task<IEnumerable<DocumentNode>> GetChildrenAsync(long parentNodeId)
    {
        return await _context.DocumentNodes
            .Where(n => n.ParentNodeId == parentNodeId && !n.IsDeleted)
            .OrderBy(n => n.EntityName)
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentNode>> GetTreeAsync(long rootNodeId = 1)
    {
        var root = await GetByIdAsync(rootNodeId);
        if (root == null) return new List<DocumentNode>();

        var tree = await GetDescendantsAsync(rootNodeId);
        return new List<DocumentNode> { root }.Concat(tree);
    }

    // Search
    public async Task<IEnumerable<DocumentNode>> SearchAsync(
        string searchTerm,
        string? idareCode = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null)
    {
        var query = _context.DocumentNodes
            .Where(n => !n.IsDeleted)
            .Where(n => n.EntityName.Contains(searchTerm) ||
                       n.EntityCode.Contains(searchTerm) ||
                       n.NormalizedDocumentNumber!.Contains(searchTerm));

        if (!string.IsNullOrEmpty(idareCode))
            query = query.Where(n => n.EntityCode.StartsWith(idareCode));

        if (dateFrom.HasValue)
            query = query.Where(n => n.DocumentDate >= dateFrom);

        if (dateTo.HasValue)
            query = query.Where(n => n.DocumentDate <= dateTo);

        return await query
            .OrderByDescending(n => n.DocumentDate)
            .Take(100)
            .ToListAsync();
    }

    // Smart Foldering - Get or Create Path
    public async Task<DocumentNode> GetOrCreatePathAsync(
        string idareCode, string idareName,
        string quyuCode, string quyuName,
        string menteqeCode, string menteqeName,
        string createdBy = "system")
    {
        // Root node
        var root = await _context.DocumentNodes
            .FirstOrDefaultAsync(n => n.NodeType == NodeType.Root && n.ParentNodeId == null)
            ?? await CreateRootNodeAsync();

        // Idare
        var idare = await _context.DocumentNodes
            .FirstOrDefaultAsync(n => n.ParentNodeId == root.NodeId && n.EntityCode == idareCode)
            ?? await CreateNodeAsync(root.NodeId, idareCode, idareName, NodeType.Idare, createdBy);

        // Quyu
        var quyu = await _context.DocumentNodes
            .FirstOrDefaultAsync(n => n.ParentNodeId == idare.NodeId && n.EntityCode == quyuCode)
            ?? await CreateNodeAsync(idare.NodeId, quyuCode, quyuName, NodeType.Quyu, createdBy);

        // Menteqe
        var menteqe = await _context.DocumentNodes
            .FirstOrDefaultAsync(n => n.ParentNodeId == quyu.NodeId && n.EntityCode == menteqeCode)
            ?? await CreateNodeAsync(quyu.NodeId, menteqeCode, menteqeName, NodeType.Menteqe, createdBy);

        return menteqe;
    }

    public async Task<DocumentNode> CreateDocumentAsync(
        long parentNodeId,
        string documentNumber,
        string documentSubject,
        DateTime documentDate,
        string createdBy = "system",
        string? normalizedNumber = null,
        string? externalNumber = null,
        DocumentSourceType? sourceType = null)
    {
        var normalized = normalizedNumber ?? NormalizeDocumentNumber(documentNumber);

        var document = new DocumentNode
        {
            ParentNodeId = parentNodeId,
            NodeType = NodeType.Document,
            EntityCode = documentNumber,
            EntityName = $"{documentDate:yyyy-MM-dd} - {documentNumber} - {documentSubject}",
            DocumentNumber = documentNumber,
            NormalizedDocumentNumber = normalized,
            DocumentDate = documentDate,
            CreatedBy = createdBy,
            Status = NodeStatus.Active,
            SourceType = sourceType ?? DocumentSourceType.InternalProject
        };

        _context.DocumentNodes.Add(document);
        await _context.SaveChangesAsync();

        // Add closure table entries
        await AddClosurePathAsync(document.NodeId, parentNodeId);
        await PropagateClosurePathsAsync(document.NodeId, parentNodeId);

        return document;
    }

    // CRUD
    public async Task<DocumentNode> AddAsync(DocumentNode node)
    {
        _context.DocumentNodes.Add(node);
        await _context.SaveChangesAsync();
        return node;
    }

    public async Task UpdateAsync(DocumentNode node)
    {
        _context.DocumentNodes.Update(node);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long nodeId)
    {
        var node = await GetByIdAsync(nodeId);
        if (node != null)
        {
            node.IsDeleted = true;
            node.Status = NodeStatus.Deleted;
            await _context.SaveChangesAsync();
        }
    }

    // CQRS üçün Query
    public IQueryable<DocumentNode> Query()
    {
        return _context.DocumentNodes
            .AsNoTracking()
            .Where(n => !n.IsDeleted)
            .AsQueryable();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    // Hierarchy helpers
    public async Task<IEnumerable<DocumentNode>> GetAncestorsAsync(long nodeId)
    {
        var ancestorIds = await _context.NodePaths
            .Where(p => p.DescendantId == nodeId)
            .Select(p => p.AncestorId)
            .ToListAsync();

        return await _context.DocumentNodes
            .Where(n => ancestorIds.Contains(n.NodeId) && !n.IsDeleted)
            .OrderBy(n => n.Depth)
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentNode>> GetDescendantsAsync(long nodeId)
    {
        var descendantIds = await _context.NodePaths
            .Where(p => p.AncestorId == nodeId && p.DescendantId != nodeId)
            .Select(p => p.DescendantId)
            .ToListAsync();

        return await _context.DocumentNodes
            .Where(n => descendantIds.Contains(n.NodeId) && !n.IsDeleted)
            .OrderBy(n => n.MaterializedPath)
            .ToListAsync();
    }

    public async Task<int> GetDepthAsync(long nodeId)
    {
        var node = await _context.DocumentNodes.FindAsync(nodeId);
        return node?.Depth ?? 0;
    }

    public async Task<IEnumerable<DocumentNode>> SearchByNormalizedNumberAsync(string normalizedNumber)
    {
        return await _context.DocumentNodes
            .Where(n => !n.IsDeleted && n.NormalizedDocumentNumber!.Contains(normalizedNumber))
            .OrderByDescending(n => n.DocumentDate)
            .Take(50)
            .ToListAsync();
    }

    // Sync Queue
    public async Task<IEnumerable<SyncQueue>> GetPendingSyncItemsAsync(string organizationCode)
    {
        return await _context.SyncQueues
            .Where(s => s.OrganizationCode == organizationCode && s.Status == SyncStatus.Pending)
            .OrderBy(s => s.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task MarkSyncAsCompletedAsync(long syncQueueId)
    {
        var item = await _context.SyncQueues.FindAsync(syncQueueId);
        if (item != null)
        {
            item.Status = SyncStatus.Completed;
            item.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task AddToSyncQueueAsync(SyncQueue syncQueue)
    {
        _context.SyncQueues.Add(syncQueue);
        await _context.SaveChangesAsync();
    }

    #region Private Helpers

    private async Task<DocumentNode> CreateRootNodeAsync()
    {
        var root = new DocumentNode
        {
            NodeType = NodeType.Root,
            EntityCode = "ROOT",
            EntityName = "Root",
            Depth = 0,
            MaterializedPath = "/1/",
            CreatedBy = "system"
        };
        _context.DocumentNodes.Add(root);
        await _context.SaveChangesAsync();
        return root;
    }

    private async Task<DocumentNode> CreateNodeAsync(long parentId, string code, string name, NodeType type, string createdBy)
    {
        var parent = await _context.DocumentNodes.FindAsync(parentId);
        var depth = parent?.Depth + 1 ?? 0;

        var node = new DocumentNode
        {
            ParentNodeId = parentId,
            NodeType = type,
            EntityCode = code,
            EntityName = name,
            Depth = depth,
            MaterializedPath = $"{parent?.MaterializedPath}{code}/",
            CreatedBy = createdBy
        };

        _context.DocumentNodes.Add(node);
        await _context.SaveChangesAsync();

        await AddClosurePathAsync(node.NodeId, parentId);

        return node;
    }

    private async Task AddClosurePathAsync(long descendantId, long ancestorId)
    {
        var path = new NodePath
        {
            AncestorId = ancestorId,
            DescendantId = descendantId,
            PathLength = 1
        };
        _context.NodePaths.Add(path);
        await _context.SaveChangesAsync();
    }

    private async Task PropagateClosurePathsAsync(long newNodeId, long parentId)
    {
        var grandparentPaths = await _context.NodePaths
            .Where(p => p.DescendantId == parentId)
            .ToListAsync();

        foreach (var gp in grandparentPaths)
        {
            var newPath = new NodePath
            {
                AncestorId = gp.AncestorId,
                DescendantId = newNodeId,
                PathLength = gp.PathLength + 1
            };
            _context.NodePaths.Add(newPath);
        }

        await _context.SaveChangesAsync();
    }

    private string NormalizeDocumentNumber(string documentNumber)
    {
        return new string(documentNumber
            .Where(c => char.IsLetterOrDigit(c))
            .ToArray())
            .ToUpperInvariant();
    }

    #endregion
}
