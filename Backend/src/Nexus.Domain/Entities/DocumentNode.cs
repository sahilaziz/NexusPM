namespace Nexus.Domain.Entities;

/// <summary>
/// Hierarchical sənəd/qovluq node-u
/// Closure Table pattern ilə hierarchy idarə olunur
/// </summary>
public class DocumentNode
{
    public long NodeId { get; set; }
    public long? ParentNodeId { get; set; }
    public NodeType NodeType { get; set; }
    
    /// <summary>
    /// Unikal kod: "AZNEFT_IB", "20_SAYLI_QUYU"
    /// </summary>
    public string EntityCode { get; set; } = null!;
    
    /// <summary>
    /// Display name: "Azneft İB", "20 saylı quyu"
    /// </summary>
    public string EntityName { get; set; } = null!;
    
    /// <summary>
    /// Hierarchical path: /1/5/12/25/
    /// </summary>
    public string? MaterializedPath { get; set; }
    
    public int Depth { get; set; }
    
    // Sənəd metadata
    public DateTime? DocumentDate { get; set; }
    public string? DocumentNumber { get; set; }
    public string? NormalizedDocumentNumber { get; set; }  // Axtarış üçün normalize edilmiş
    public string? DocumentSubject { get; set; }
    public string? ExternalDocumentNumber { get; set; }  // Xarici sənəd nömrəsi (məktublar üçün)
    public DocumentSourceType? SourceType { get; set; }  // Daxili/Xarici məktub/Layihə
    
    /// <summary>
    /// UNC path: \\Server\Nexus\Documents\...
    /// </summary>
    public string? FileSystemPath { get; set; }
    
    public NodeStatus Status { get; set; } = NodeStatus.Active;
    
    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "system";
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
    
    // Navigation properties
    public DocumentNode? Parent { get; set; }
    public ICollection<DocumentNode> Children { get; set; } = new List<DocumentNode>();
    public ICollection<NodePath> Ancestors { get; set; } = new List<NodePath>();
    public ICollection<NodePath> Descendants { get; set; } = new List<NodePath>();
    public Project? Project { get; set; }
}

public enum NodeType
{
    Root,      // Root node (sadəcə 1 dənə)
    Idare,     // İdarə (Azneft İB, Azpetrol İB)
    Quyu,      // Quyu (20 saylı quyu)
    Menteqe,   // Yaşayış məntəqəsi
    Document   // Sənəd (PDF fayl)
}

public enum NodeStatus
{
    Active,
    Archived,
    Deleted
}

public enum DocumentSourceType
{
    IncomingLetter,   // Daxil olan məktub
    InternalProject,  // Daxili layihə
    ExternalDocument  // Xarici sənəd
}
