using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Nexus.Application.Interfaces.Repositories;

namespace Nexus.Application.CQRS.Queries;

/// <summary>
/// CQRS Query - Document oxumaq
/// Oxuma əməliyyatları burada (Read Model)
/// Optimized for READ performance
/// </summary>
public class GetDocumentQuery : IRequest<DocumentDto?>
{
    public long DocumentId { get; set; }
    public string OrganizationCode { get; set; } = null!;
}

public class GetDocumentQueryHandler : IRequestHandler<GetDocumentQuery, DocumentDto?>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDistributedCache _cache;  // NCache/SQL Cache
    private readonly ILogger<GetDocumentQueryHandler> _logger;

    public GetDocumentQueryHandler(
        IDocumentRepository documentRepository,
        IDistributedCache cache,
        ILogger<GetDocumentQueryHandler> logger)
    {
        _documentRepository = documentRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<DocumentDto?> Handle(GetDocumentQuery request, CancellationToken cancellationToken)
    {
        // 1. Cache-dən yoxla (sub-millisecond response)
        var cacheKey = $"doc:{request.OrganizationCode}:{request.DocumentId}";
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        
        if (!string.IsNullOrEmpty(cached))
        {
            _logger.LogDebug("Cache hit for document {DocumentId}", request.DocumentId);
            return JsonSerializer.Deserialize<DocumentDto>(cached);
        }

        // 2. Database-dən oxu (Read Replica istifadə edə bilər)
        var document = await _documentRepository.GetByIdAsync(request.DocumentId);
        
        if (document == null || document.OrganizationCode != request.OrganizationCode)
            return null;

        var dto = new DocumentDto
        {
            Id = document.NodeId,
            Title = document.EntityName,
            CreatedAt = document.CreatedAt,
            CreatedBy = document.CreatedBy
        };

        // 3. Cache-ə yaz (5 dəqiqəlik)
        await _cache.SetStringAsync(
            cacheKey, 
            JsonSerializer.Serialize(dto),
            new DistributedCacheEntryOptions 
            { 
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) 
            },
            cancellationToken);

        return dto;
    }
}

/// <summary>
/// Search query - optimize edilmiş axtarış
/// SQL LIKE yox, full-text search
/// </summary>
public class SearchDocumentsQuery : IRequest<SearchResult<DocumentDto>>
{
    public string SearchTerm { get; set; } = null!;
    public string OrganizationCode { get; set; } = null!;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SearchDocumentsQueryHandler : IRequestHandler<SearchDocumentsQuery, SearchResult<DocumentDto>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDistributedCache _cache;

    public SearchDocumentsQueryHandler(
        IDocumentRepository documentRepository,
        IDistributedCache cache)
    {
        _documentRepository = documentRepository;
        _cache = cache;
    }

    public async Task<SearchResult<DocumentDto>> Handle(SearchDocumentsQuery request, CancellationToken cancellationToken)
    {
        // Cache axtarış nəticələrini (qısa müddətli)
        var cacheKey = $"search:{request.OrganizationCode}:{request.SearchTerm.GetHashCode()}:p{request.PageNumber}";
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        
        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<SearchResult<DocumentDto>>(cached)!;
        }

        // Database query (IQueryable ilə optimize)
        var query = _documentRepository.Query()  // IQueryable döndürür
            .Where(d => d.OrganizationCode == request.OrganizationCode)
            .Where(d => d.EntityName.Contains(request.SearchTerm) || 
                       d.EntityCode.Contains(request.SearchTerm))
            .OrderByDescending(d => d.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DocumentDto
            {
                Id = d.NodeId,
                Title = d.EntityName,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var result = new SearchResult<DocumentDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        // Cache-lə (1 dəqiqəlik - axtarış tez-tez dəyişir)
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(result),
            new DistributedCacheEntryOptions 
            { 
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) 
            },
            cancellationToken);

        return result;
    }
}

public class SearchResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
