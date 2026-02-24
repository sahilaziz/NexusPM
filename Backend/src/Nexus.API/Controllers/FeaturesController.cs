using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace Nexus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class FeaturesController : ControllerBase
{
    private readonly IFeatureManager _featureManager;

    public FeaturesController(IFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllFeatures()
    {
        var features = new Dictionary<string, bool>
        {
            ["NewSearchAlgorithm"] = await _featureManager.IsEnabledAsync("NewSearchAlgorithm"),
            ["AdvancedCaching"] = await _featureManager.IsEnabledAsync("AdvancedCaching"),
            ["RealTimeNotifications"] = await _featureManager.IsEnabledAsync("RealTimeNotifications"),
            ["BulkUpload"] = await _featureManager.IsEnabledAsync("BulkUpload")
        };

        return Ok(features);
    }

    [HttpGet("{featureName}")]
    public async Task<IActionResult> IsEnabled(string featureName)
    {
        var isEnabled = await _featureManager.IsEnabledAsync(featureName);
        return Ok(new { Feature = featureName, IsEnabled = isEnabled });
    }
}

/// <summary>
/// Feature usage example in service
/// </summary>
public class DocumentSearchService
{
    private readonly IFeatureManager _featureManager;
    private readonly IDocumentRepository _repository;

    public DocumentSearchService(
        IFeatureManager featureManager,
        IDocumentRepository repository)
    {
        _featureManager = featureManager;
        _repository = repository;
    }

    public async Task<List<Document>> SearchAsync(string query)
    {
        // Tədricən yeni alqoritmə keçid
        if (await _featureManager.IsEnabledAsync("NewSearchAlgorithm"))
        {
            // Yeni axtarış (Azure Cognitive Search)
            return await SearchWithNewAlgorithmAsync(query);
        }

        // Köhnə axtarış (SQL LIKE)
        return await _repository.SearchAsync(query).ToList();
    }

    private Task<List<Document>> SearchWithNewAlgorithmAsync(string query)
    {
        // Yeni implementasiya
        throw new NotImplementedException();
    }
}
