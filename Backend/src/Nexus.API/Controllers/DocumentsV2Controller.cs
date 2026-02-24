using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.CQRS.Commands;
using Nexus.Application.CQRS.Queries;

namespace Nexus.API.Controllers;

/// <summary>
/// Documents API v2 - CQRS Pattern ilə
/// </summary>
[ApiController]
[Route("api/v2/[controller]")]
[Authorize]
public class DocumentsV2Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DocumentsV2Controller> _logger;

    public DocumentsV2Controller(
        IMediator mediator,
        ILogger<DocumentsV2Controller> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Sənəd yarat (CQRS Command)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateDocumentRequest request)
    {
        try
        {
            var command = new CreateDocumentCommand
            {
                Title = request.Title,
                ParentNodeId = request.ParentNodeId,
                OrganizationCode = User.FindFirst("org")?.Value ?? "default",
                CreatedBy = User.Identity?.Name ?? "unknown",
                FileStream = request.File.OpenReadStream(),
                FileName = request.File.FileName,
                ContentType = request.File.ContentType
            };

            var result = await _mediator.Send(command);
            
            return CreatedAtAction(
                nameof(GetById), 
                new { id = result.Id }, 
                result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Sənəd əldə et (CQRS Query)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var query = new GetDocumentQuery
        {
            DocumentId = id,
            OrganizationCode = User.FindFirst("org")?.Value ?? "default"
        };

        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Sənəd axtar (CQRS Query with Cache)
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] int page = 1)
    {
        var query = new SearchDocumentsQuery
        {
            SearchTerm = term,
            OrganizationCode = User.FindFirst("org")?.Value ?? "default",
            PageNumber = page,
            PageSize = 20
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }
}

public class CreateDocumentRequest
{
    public string Title { get; set; } = null!;
    public long ParentNodeId { get; set; }
    public IFormFile File { get; set; } = null!;
}
