using System.Text.RegularExpressions;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;

namespace Nexus.Application.Services;

/// <summary>
/// Sənəd identifikator idarəetməsi - Daxili/Xarici sənədlər üçün
/// </summary>
public interface IDocumentIdentifierService
{
    /// <summary>
    /// Yeni sənəd identifikatoru yarat
    /// </summary>
    Task<DocumentIdentifierResult> CreateIdentifierAsync(
        DocumentSourceType sourceType,
        string? externalDocumentNumber = null,
        string? idareCode = null);

    /// <summary>
    /// Xarici sənəd nömrəsini normalize et (axtarış üçün)
    /// </summary>
    string NormalizeDocumentNumber(string documentNumber);

    /// <summary>
    /// Sənəd nömrəsinin unikal olduğunu yoxla
    /// </summary>
    Task<bool> IsDocumentNumberUniqueAsync(string documentNumber);

    /// <summary>
    /// Smart axtarış - sənəd nömrəsini normalizasiya edib axtar
    /// </summary>
    Task<IEnumerable<DocumentNode>> SearchByDocumentNumberAsync(string searchTerm);
}

public class DocumentIdentifierService : IDocumentIdentifierService
{
    private readonly IDocumentRepository _repository;
    private readonly ILogger<DocumentIdentifierService> _logger;

    public DocumentIdentifierService(
        IDocumentRepository repository,
        ILogger<DocumentIdentifierService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<DocumentIdentifierResult> CreateIdentifierAsync(
        DocumentSourceType sourceType,
        string? externalDocumentNumber = null,
        string? idareCode = null)
    {
        return sourceType switch
        {
            DocumentSourceType.IncomingLetter => await CreateIncomingLetterIdentifierAsync(externalDocumentNumber!),
            DocumentSourceType.InternalProject => await CreateInternalProjectIdentifierAsync(idareCode!),
            DocumentSourceType.ExternalDocument => await CreateExternalDocumentIdentifierAsync(externalDocumentNumber!, idareCode!),
            _ => throw new ArgumentException("Unknown document source type")
        };
    }

    /// <summary>
    /// Daxil olan məktub üçün identifikator - istifadəçinin daxil etdiyi nömrəni saxla
    /// Format: 1-4-8\3-2-1243\2026, 45-а\123\2026 və s.
    /// </summary>
    private async Task<DocumentIdentifierResult> CreateIncomingLetterIdentifierAsync(string externalDocumentNumber)
    {
        if (string.IsNullOrWhiteSpace(externalDocumentNumber))
        {
            throw new ArgumentException("Daxil olan məktub nömrəsi tələb olunur");
        }

        // Nömrənin unikal olduğunu yoxla
        var normalized = NormalizeDocumentNumber(externalDocumentNumber);
        var existing = await _repository.SearchAsync(normalized, null, null, null);
        
        if (existing.Any(d => NormalizeDocumentNumber(d.DocumentNumber!) == normalized))
        {
            throw new InvalidOperationException(
                $"Bu sənəd nömrəsi artıq istifadə olunur: {externalDocumentNumber}");
        }

        _logger.LogInformation(
            "Daxil olan məktub identifikatoru yaradıldı: {DocumentNumber}", 
            externalDocumentNumber);

        return new DocumentIdentifierResult
        {
            DocumentNumber = externalDocumentNumber.Trim().ToUpper(),
            NormalizedNumber = normalized,
            SourceType = DocumentSourceType.IncomingLetter,
            DisplayName = $"Məktub №{externalDocumentNumber}"
        };
    }

    /// <summary>
    /// Daxili layihə üçün identifikator - avtomatik yarat
    /// Format: PRJ-{İDARƏ}-{İL}-{SAY}
    /// </summary>
    private async Task<DocumentIdentifierResult> CreateInternalProjectIdentifierAsync(string idareCode)
    {
        var year = DateTime.Now.Year;
        var prefix = $"PRJ-{idareCode}-{year}-";
        
        // Sonuncu nömrəni tap
        var lastNumber = await GetLastProjectNumberAsync(idareCode, year);
        var newNumber = lastNumber + 1;
        
        var documentNumber = $"{prefix}{newNumber:D4}";

        _logger.LogInformation(
            "Daxili layihə identifikatoru yaradıldı: {DocumentNumber}", 
            documentNumber);

        return new DocumentIdentifierResult
        {
            DocumentNumber = documentNumber,
            NormalizedNumber = NormalizeDocumentNumber(documentNumber),
            SourceType = DocumentSourceType.InternalProject,
            DisplayName = $"Layihə №{documentNumber}"
        };
    }

    /// <summary>
    /// Xarici sənəd üçün identifikator
    /// </summary>
    private async Task<DocumentIdentifierResult> CreateExternalDocumentIdentifierAsync(
        string externalNumber, string idareCode)
    {
        if (string.IsNullOrWhiteSpace(externalNumber))
        {
            throw new ArgumentException("Xarici sənəd nömrəsi tələb olunur");
        }

        // Sistemə xarici nömrəni və öz nömrəmizi saxla
        var year = DateTime.Now.Year;
        var internalId = await GetLastExternalDocNumberAsync(idareCode, year);
        
        var documentNumber = $"EXT-{idareCode}-{year}-{internalId:D4}";

        _logger.LogInformation(
            "Xarici sənəd identifikatoru yaradıldı: {InternalId}, Xarici: {ExternalId}", 
            documentNumber, externalNumber);

        return new DocumentIdentifierResult
        {
            DocumentNumber = documentNumber,
            ExternalNumber = externalNumber.Trim().ToUpper(),
            NormalizedNumber = NormalizeDocumentNumber(documentNumber),
            SourceType = DocumentSourceType.ExternalDocument,
            DisplayName = $"Xarici sənəd №{externalNumber}"
        };
    }

    /// <summary>
    /// Sənəd nömrəsini normalize et - axtarış üçün
    /// Hərf və rəqəmləri saxla, xüsusi simvolları sil
    /// </summary>
    public string NormalizeDocumentNumber(string documentNumber)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
            return string.Empty;

        // Bütün simvolları böyük hərfə çevir
        var normalized = documentNumber.ToUpperInvariant();
        
        // Xüsusi simvolları boşluqla əvəz et (axtarış üçün)
        // 1-4-8\3-2-1243\2026 → "1 4 8 3 2 1243 2026"
        normalized = Regex.Replace(normalized, @"[^\w\d]", " ");
        
        // Çoxlu boşluqları təkə endir
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        return normalized;
    }

    /// <summary>
    /// Axtarış termini üçün normalize - simvolları ignor et
    /// </summary>
    public string NormalizeSearchTerm(string searchTerm)
    {
        return NormalizeDocumentNumber(searchTerm);
    }

    public async Task<bool> IsDocumentNumberUniqueAsync(string documentNumber)
    {
        var normalized = NormalizeDocumentNumber(documentNumber);
        var existing = await _repository.SearchAsync(documentNumber, null, null, null);
        
        return !existing.Any(d => 
            NormalizeDocumentNumber(d.DocumentNumber!) == normalized);
    }

    public async Task<IEnumerable<DocumentNode>> SearchByDocumentNumberAsync(string searchTerm)
    {
        // İlk önce tam axtarış
        var results = await _repository.SearchAsync(searchTerm, null, null, null);
        
        // Əgər tapılmadısa, normalize edib axtar
        if (!results.Any())
        {
            var normalized = NormalizeSearchTerm(searchTerm);
            // Repository-də normalize olunmuş formada axtar
            results = await _repository.SearchByNormalizedNumberAsync(normalized);
        }

        return results.Where(d => d.NodeType == NodeType.Document);
    }

    private async Task<int> GetLastProjectNumberAsync(string idareCode, int year)
    {
        // PRJ-{İDARƏ}-{İL}-XXXX formatında sonuncu nömrəni tap
        var prefix = $"PRJ-{idareCode}-{year}-";
        
        // Bu prefix ilə başlayan sənədləri tap
        var documents = await _repository.SearchAsync(prefix, null, null, null);
        
        var maxNumber = documents
            .Where(d => d.DocumentNumber?.StartsWith(prefix) == true)
            .Select(d =>
            {
                var parts = d.DocumentNumber?.Split('-');
                if (parts != null && parts.Length >= 4 && int.TryParse(parts[3], out var num))
                    return num;
                return 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return maxNumber;
    }

    private async Task<int> GetLastExternalDocNumberAsync(string idareCode, int year)
    {
        var prefix = $"EXT-{idareCode}-{year}-";
        
        var documents = await _repository.SearchAsync(prefix, null, null, null);
        
        var maxNumber = documents
            .Where(d => d.DocumentNumber?.StartsWith(prefix) == true)
            .Select(d =>
            {
                var parts = d.DocumentNumber?.Split('-');
                if (parts != null && parts.Length >= 4 && int.TryParse(parts[3], out var num))
                    return num;
                return 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return maxNumber + 1;
    }
}

public enum DocumentSourceType
{
    /// <summary>
    /// Daxil olan məktub - istifadəçi nömrəni daxil edir
    /// </summary>
    IncomingLetter,
    
    /// <summary>
    /// Daxili layihə - sistem avtomatik nömrə verir
    /// </summary>
    InternalProject,
    
    /// <summary>
    /// Xarici sənəd - xarici nömrə + daxili nömrə
    /// </summary>
    ExternalDocument
}

public class DocumentIdentifierResult
{
    public string DocumentNumber { get; set; } = string.Empty;
    public string? ExternalNumber { get; set; }
    public string NormalizedNumber { get; set; } = string.Empty;
    public DocumentSourceType SourceType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
