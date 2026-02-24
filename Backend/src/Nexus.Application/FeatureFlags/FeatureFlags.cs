namespace Nexus.Application.FeatureFlags;

/// <summary>
/// Feature Flags - Tədricən deployment üçün
/// </summary>
public static class Features
{
    public const string NewSearchAlgorithm = "NewSearchAlgorithm";
    public const string AdvancedCaching = "AdvancedCaching";
    public const string RealTimeNotifications = "RealTimeNotifications";
    public const string BulkUpload = "BulkUpload";
}

/// <summary>
/// Feature Manager
/// </summary>
public interface IFeatureManager
{
    Task<bool> IsEnabledAsync(string featureName);
}
