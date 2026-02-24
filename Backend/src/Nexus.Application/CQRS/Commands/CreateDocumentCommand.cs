using MediatR;
using Nexus.Domain.Entities;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Application.Interfaces.Services;

namespace Nexus.Application.CQRS.Commands;

/// <summary>
/// CQRS Command - Document yaratmaq
/// Yazma əməliyyatları burada (Write Model)
/// </summary>
public class CreateDocumentCommand : IRequest<DocumentDto>
{
    public string Title { get; set; } = null!;
    public long ParentNodeId { get; set; }
    public string OrganizationCode { get; set; } = null!;
    public string CreatedBy { get; set; } = null!;
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
}

public class CreateDocumentCommandHandler : IRequestHandler<CreateDocumentCommand, DocumentDto>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IStorageService _storageService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CreateDocumentCommandHandler> _logger;

    public CreateDocumentCommandHandler(
        IDocumentRepository documentRepository,
        IStorageService storageService,
        IEventBus eventBus,
        ILogger<CreateDocumentCommandHandler> logger)
    {
        _documentRepository = documentRepository;
        _storageService = storageService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<DocumentDto> Handle(CreateDocumentCommand request, CancellationToken cancellationToken)
    {
        // 1. File-ı storage-a yaz
        var storagePath = await _storageService.SaveFileAsync(
            request.FileStream, 
            request.FileName,
            request.ContentType,
            request.OrganizationCode);

        // 2. Database record yarat
        var document = new DocumentNode
        {
            EntityName = request.Title,
            ParentNodeId = request.ParentNodeId,
            EntityCode = GenerateDocumentCode(),
            OrganizationCode = request.OrganizationCode,
            FileSystemPath = storagePath,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            Status = NodeStatus.Active
        };

        await _documentRepository.AddAsync(document);
        await _documentRepository.SaveChangesAsync();

        // 3. Event publish et (asinxron)
        await _eventBus.PublishAsync(new DocumentCreatedEvent
        {
            DocumentId = document.NodeId,
            Title = document.EntityName,
            OrganizationCode = document.OrganizationCode,
            CreatedBy = document.CreatedBy,
            CreatedAt = document.CreatedAt
        });

        _logger.LogInformation("Document {DocumentId} created", document.NodeId);

        return new DocumentDto
        {
            Id = document.NodeId,
            Title = document.EntityName,
            CreatedAt = document.CreatedAt
        };
    }

    private string GenerateDocumentCode() => 
        $"DOC-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
}
