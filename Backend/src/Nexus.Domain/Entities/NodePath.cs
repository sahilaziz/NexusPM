namespace Nexus.Domain.Entities;

/// <summary>
/// Closure Table - Hierarchical əlaqələri saxlayır
/// Ancestor → Descendant (0 depth = özü, 1 = uşaq, 2 = nəvə)
/// </summary>
public class NodePath
{
    public long AncestorId { get; set; }
    public long DescendantId { get; set; }
    
    /// <summary>
    /// 0 = özü, 1 = birbaşa uşaq, 2 = nəvə və s.
    /// </summary>
    public int Depth { get; set; }
    
    // Navigation
    public DocumentNode Ancestor { get; set; } = null!;
    public DocumentNode Descendant { get; set; } = null!;
}
