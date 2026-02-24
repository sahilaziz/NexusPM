namespace Nexus.Application;

/// <summary>
/// Data Transfer Object - Document
/// </summary>
public class DocumentDto
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;
    public string? DocumentNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
}

/// <summary>
/// Event DTO - Document yaradıldıqda göndərilir
/// </summary>
public class DocumentCreatedEvent : IEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public long DocumentId { get; set; }
    public string Title { get; set; } = null!;
    public string OrganizationCode { get; set; } = null!;
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public interface IEvent
{
    Guid EventId { get; set; }
}
