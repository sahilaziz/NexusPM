using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Nexus.API.Hubs;
using Nexus.Application.Services;
using Nexus.Domain.Entities;

namespace Nexus.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentIdentifierService _identifierService;
    private readonly IHubContext<SyncHub> _hubContext;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        IDocumentIdentifierService identifierService,
        IHubContext<SyncHub> hubContext,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _identifierService = identifierService;
        _hubContext = hubContext;
        _logger = logger;
    }

    private string CurrentUserId => User.FindFirst("sub")?.Value ?? "unknown";
    private string OrganizationCode => User.FindFirst("organization")?.Value ?? "default";

    /// <summary>
    /// Smart Foldering ilə sənəd yarat
    /// SourceType: IncomingLetter (daxil olan məktub) və ya InternalProject (daxili layihə)
    /// </summary>
    [HttpPost("create-with-path")]
    [Authorize(Policy = "RequireUser")]
    public async Task<ActionResult<DocumentNode>> CreateWithPath([FromBody] CreateDocumentRequest request)
    {
        try
        {
            request.CreatedBy = CurrentUserId;

            var document = await _documentService.CreateDocumentWithPathAsync(request);

            // Broadcast to connected clients via SignalR
            await SyncHub.BroadcastDocumentCreated(
                _hubContext, 
                document, 
                OrganizationCode);

            _logger.LogInformation(
                "Document {DocumentId} created by {UserId} in organization {Org}",
                document.NodeId, CurrentUserId, OrganizationCode);

            return Ok(new
            {
                Success = true,
                Data = document,
                Message = "Sənəd uğurla yaradıldı"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create document");
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Daxili layihə yarat (avtomatik ID ilə)
    /// </summary>
    [HttpPost("create-internal-project")]
    [Authorize(Policy = "RequireUser")]
    public async Task<ActionResult<DocumentNode>> CreateInternalProject([FromBody] CreateInternalProjectRequest request)
    {
        try
        {
            var docRequest = new CreateDocumentRequest
            {
                IdareCode = request.IdareCode,
                IdareName = request.IdareName,
                QuyuCode = request.QuyuCode,
                QuyuName = request.QuyuName,
                MenteqeCode = request.MenteqeCode,
                MenteqeName = request.MenteqeName,
                DocumentDate = request.DocumentDate,
                DocumentSubject = request.ProjectName,
                SourceType = DocumentSourceType.InternalProject,
                CreatedBy = CurrentUserId
            };

            var document = await _documentService.CreateDocumentWithPathAsync(docRequest);

            await SyncHub.BroadcastDocumentCreated(_hubContext, document, OrganizationCode);

            return Ok(new
            {
                Success = true,
                Data = document,
                Message = $"Daxili layihə yaradıldı: {document.DocumentNumber}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create internal project");
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Daxil olan məktub yarat (xarici nömrə ilə)
    /// </summary>
    [HttpPost("create-incoming-letter")]
    [Authorize(Policy = "RequireUser")]
    public async Task<ActionResult<DocumentNode>> CreateIncomingLetter([FromBody] CreateIncomingLetterRequest request)
    {
        try
        {
            // Sənəd nömrəsinin unikal olduğunu yoxla
            var isUnique = await _identifierService.IsDocumentNumberUniqueAsync(request.DocumentNumber);
            if (!isUnique)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = $"Bu sənəd nömrəsi artıq istifadə olunur: {request.DocumentNumber}"
                });
            }

            var docRequest = new CreateDocumentRequest
            {
                IdareCode = request.IdareCode,
                IdareName = request.IdareName,
                QuyuCode = request.QuyuCode,
                QuyuName = request.QuyuName,
                MenteqeCode = request.MenteqeCode,
                MenteqeName = request.MenteqeName,
                DocumentDate = request.DocumentDate,
                DocumentSubject = request.Subject,
                SourceType = DocumentSourceType.IncomingLetter,
                ExternalDocumentNumber = request.DocumentNumber,
                CreatedBy = CurrentUserId
            };

            var document = await _documentService.CreateDocumentWithPathAsync(docRequest);

            await SyncHub.BroadcastDocumentCreated(_hubContext, document, OrganizationCode);

            return Ok(new
            {
                Success = true,
                Data = document,
                Message = "Daxil olan məktub qeydə alındı"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create incoming letter");
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Sənəd nömrəsini yoxla (unikal olduğunu)
    /// </summary>
    [HttpGet("check-document-number")]
    [Authorize]
    public async Task<ActionResult> CheckDocumentNumber([FromQuery] string number)
    {
        var isUnique = await _identifierService.IsDocumentNumberUniqueAsync(number);
        var normalized = _identifierService.NormalizeDocumentNumber(number);

        return Ok(new
        {
            IsUnique = isUnique,
            Original = number,
            Normalized = normalized,
            Message = isUnique ? "Bu nömrə istifadə edilə bilər" : "Bu nömrə artıq istifadə olunur"
        });
    }

    /// <summary>
    /// Smart axtarış - sənəd nömrəsinə görə
    /// Format: 1-4-8\3-2-1243\2026 və ya hər hansı simvol kombinasiyası
    /// </summary>
    [HttpGet("search-by-number")]
    [Authorize]
    public async Task<ActionResult> SearchByNumber([FromQuery] string number)
    {
        var results = await _identifierService.SearchByDocumentNumberAsync(number);
        
        return Ok(new
        {
            Success = true,
            Count = results.Count(),
            SearchTerm = number,
            Normalized = _identifierService.NormalizeDocumentNumber(number),
            Data = results
        });
    }

    /// <summary>
    /// Qovluq tree-sini əldə et
    /// </summary>
    [HttpGet("tree")]
    public async Task<ActionResult<IEnumerable<DocumentNode>>> GetTree([FromQuery] long parentId = 1)
    {
        var items = await _documentService.GetFolderTreeAsync(parentId);
        return Ok(items);
    }

    /// <summary>
    /// Sənəd axtarışı
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<DocumentNode>>> Search(
        [FromQuery] string term,
        [FromQuery] string? idareCode = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        var request = new SearchRequest
        {
            SearchTerm = term,
            IdareCode = idareCode,
            DateFrom = dateFrom,
            DateTo = dateTo
        };
        
        var results = await _documentService.SearchDocumentsAsync(request);
        return Ok(results);
    }

    /// <summary>
    /// Sənəd detalları
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentNode>> Get(long id)
    {
        var doc = await _documentService.GetDocumentAsync(id);
        if (doc == null)
            return NotFound();
        
        return Ok(doc);
    }
}

// DTOs
public class CreateInternalProjectRequest
{
    public string IdareCode { get; set; } = string.Empty;
    public string IdareName { get; set; } = string.Empty;
    public string QuyuCode { get; set; } = string.Empty;
    public string QuyuName { get; set; } = string.Empty;
    public string MenteqeCode { get; set; } = string.Empty;
    public string MenteqeName { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public string ProjectName { get; set; } = string.Empty;
}

public class CreateIncomingLetterRequest
{
    public string IdareCode { get; set; } = string.Empty;
    public string IdareName { get; set; } = string.Empty;
    public string QuyuCode { get; set; } = string.Empty;
    public string QuyuName { get; set; } = string.Empty;
    public string MenteqeCode { get; set; } = string.Empty;
    public string MenteqeName { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;  // Məs: 1-4-8\3-2-1243\2026
    public string Subject { get; set; } = string.Empty;
}
