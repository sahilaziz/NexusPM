using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Data.SeedData;

/// <summary>
/// Default system labels for new projects
/// </summary>
public static class DefaultLabels
{
    /// <summary>
    /// Sistem etiketləri (hər təşkilat üçün)
    /// </summary>
    public static readonly List<TaskLabel> SystemLabels = new()
    {
        new TaskLabel
        {
            Name = "Bug",
            Description = "Xəta və ya problemli davranış",
            Color = "#EF4444", // Red-500
            SortOrder = 1,
            IsSystem = true,
            IsActive = true
        },
        new TaskLabel
        {
            Name = "Feature",
            Description = "Yeni funksionallıq",
            Color = "#3B82F6", // Blue-500
            SortOrder = 2,
            IsSystem = true,
            IsActive = true
        },
        new TaskLabel
        {
            Name = "Improvement",
            Description = "Təkmilləşdirmə",
            Color = "#10B981", // Emerald-500
            SortOrder = 3,
            IsSystem = true,
            IsActive = true
        },
        new TaskLabel
        {
            Name = "Documentation",
            Description = "Sənədləşmə işləri",
            Color = "#8B5CF6", // Violet-500
            SortOrder = 4,
            IsSystem = true,
            IsActive = true
        },
        new TaskLabel
        {
            Name = "Design",
            Description = "UI/UX dizayn işləri",
            Color = "#EC4899", // Pink-500
            SortOrder = 5,
            IsSystem = true,
            IsActive = true
        },
        new TaskLabel
        {
            Name = "Backend",
            Description = "Server tərəfi işləri",
            Color = "#6366F1", // Indigo-500
            SortOrder = 6,
            IsSystem = true,
            IsActive = true
        },
        new TaskLabel
        {
            Name = "Frontend",
            Description = "Client tərəfi işləri",
            Color = "#F59E0B", // Amber-500
            SortOrder = 7,
            IsSystem = true,
            IsActive = true
        },
        new TaskLabel
        {
            Name = "Urgent",
            Description = "Təcili",
            Color = "#DC2626", // Red-600
            SortOrder = 8,
            IsSystem = true,
            IsActive = true
        },
        new TaskLabel
        {
            Name = "Low Priority",
            Description = "Aşağı prioritet",
            Color = "#6B7280", // Gray-500
            SortOrder = 9,
            IsSystem = true,
            IsActive = true
        },
        new TaskLabel
        {
            Name = "Research",
            Description = "Araşdırma işləri",
            Color = "#14B8A6", // Teal-500
            SortOrder = 10,
            IsSystem = true,
            IsActive = true
        },
        new TaskLabel
        {
            Name = "Testing",
            Description = "Test işləri",
            Color = "#84CC16", // Lime-500
            SortOrder = 11,
            IsSystem = true,
            IsActive = true
        },
        new TaskLabel
        {
            Name = "Refactoring",
            Description = "Kod təmizləmə və optimallaşdırma",
            Color = "#06B6D4", // Cyan-500
            SortOrder = 12,
            IsSystem = true,
            IsActive = true
        }
    };

    /// <summary>
    /// Neft-Qaz sektoru üçün xüsusi etiketlər
    /// </summary>
    public static readonly List<TaskLabel> OilGasLabels = new()
    {
        new TaskLabel
        {
            Name = "Safety",
            Description = "Təhlükəsizlik məsələləri",
            Color = "#B91C1C", // Red-700
            SortOrder = 20,
            IsSystem = false,
            IsActive = true
        },
        new TaskLabel
        {
            Name = "Compliance",
            Description = "Qanunvericilik və standartlar",
            Color = "#1E40AF", // Blue-800
            SortOrder = 21,
            IsSystem = false,
            IsActive = true
        },
        new TaskLabel
        {
            Name = "Environmental",
            Description = "Ətraf mühit məsələləri",
            Color = "#15803D", // Green-700
            SortOrder = 22,
            IsSystem = false,
            IsActive = true
        },
        new TaskLabel
        {
            Name = "Inspection",
            Description = "Yoxlama və təftiş",
            Color = "#A16207", // Yellow-700
            SortOrder = 23,
            IsSystem = false,
            IsActive = true
        },
        new TaskLabel
        {
            Name = "Maintenance",
            Description = "Təmir və baxım",
            Color = "#4338CA", // Indigo-700
            SortOrder = 24,
            IsSystem = false,
            IsActive = true
        }
    };

    /// <summary>
    /// Default rənglər (custom label yaratmaq üçün)
    /// </summary>
    public static readonly List<string> DefaultColors = new()
    {
        "#EF4444", // Red
        "#F97316", // Orange
        "#F59E0B", // Amber
        "#84CC16", // Lime
        "#22C55E", // Green
        "#10B981", // Emerald
        "#14B8A6", // Teal
        "#06B6D4", // Cyan
        "#0EA5E9", // Sky
        "#3B82F6", // Blue
        "#6366F1", // Indigo
        "#8B5CF6", // Violet
        "#A855F7", // Purple
        "#D946EF", // Fuchsia
        "#EC4899", // Pink
        "#F43F5E", // Rose
        "#6B7280", // Gray
        "#1F2937", // Dark Gray
    };
}
