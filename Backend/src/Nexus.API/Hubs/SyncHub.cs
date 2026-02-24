using Microsoft.AspNetCore.SignalR;
using Nexus.Application.Services;
using Nexus.Domain.Entities;

namespace Nexus.API.Hubs;

/// <summary>
/// Real-time synchronization hub for document changes
/// </summary>
public class SyncHub : Hub
{
    private readonly ILogger<SyncHub> _logger;
    private readonly IDocumentService _documentService;
    private static readonly Dictionary<string, UserConnection> _connections = new();

    public SyncHub(
        ILogger<SyncHub> logger,
        IDocumentService documentService)
    {
        _logger = logger;
        _documentService = documentService;
    }

    /// <summary>
    /// Client joins their organization group for targeted updates
    /// </summary>
    public async Task JoinOrganization(string organizationCode)
    {
        var connectionId = Context.ConnectionId;
        
        _connections[connectionId] = new UserConnection
        {
            ConnectionId = connectionId,
            OrganizationCode = organizationCode,
            ConnectedAt = DateTime.UtcNow
        };

        await Groups.AddToGroupAsync(connectionId, organizationCode);
        _logger.LogInformation("Client {ConnectionId} joined organization {OrgCode}", 
            connectionId, organizationCode);

        // Send pending sync items to client
        var pendingItems = await _documentService.GetPendingSyncItemsAsync(organizationCode);
        await Clients.Caller.SendAsync("SyncPendingItems", pendingItems);
    }

    /// <summary>
    /// Client confirms receipt of sync item
    /// </summary>
    public async Task ConfirmSync(long syncQueueId)
    {
        await _documentService.MarkSyncAsCompletedAsync(syncQueueId);
        _logger.LogDebug("Sync {SyncId} confirmed by {ConnectionId}", 
            syncQueueId, Context.ConnectionId);
    }

    /// <summary>
    /// Client requests full tree refresh
    /// </summary>
    public async Task RequestTreeRefresh(long parentNodeId)
    {
        var tree = await _documentService.GetFolderTreeAsync(parentNodeId);
        await Clients.Caller.SendAsync("TreeRefreshed", tree);
    }

    /// <summary>
    /// Broadcast document created to all organization members
    /// </summary>
    public static async Task BroadcastDocumentCreated(
        IHubContext<SyncHub> hubContext, 
        DocumentNode document, 
        string organizationCode)
    {
        await hubContext.Clients
            .Group(organizationCode)
            .SendAsync("DocumentCreated", new DocumentSyncDto
            {
                NodeId = document.NodeId,
                ParentNodeId = document.ParentNodeId,
                NodeType = document.NodeType,
                EntityCode = document.EntityCode,
                EntityName = document.EntityName,
                MaterializedPath = document.MaterializedPath,
                DocumentDate = document.DocumentDate,
                DocumentNumber = document.DocumentNumber,
                Timestamp = DateTime.UtcNow
            });
    }

    /// <summary>
    /// Notify clients about sync queue updates
    /// </summary>
    public static async Task BroadcastSyncQueueUpdated(
        IHubContext<SyncHub> hubContext,
        long syncQueueId,
        string organizationCode)
    {
        await hubContext.Clients
            .Group(organizationCode)
            .SendAsync("SyncQueueUpdated", new { SyncQueueId = syncQueueId });
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        _connections.Remove(connectionId);
        
        _logger.LogInformation("Client disconnected: {ConnectionId}", connectionId);
        return base.OnDisconnectedAsync(exception);
    }
}

public class UserConnection
{
    public string ConnectionId { get; set; } = string.Empty;
    public string OrganizationCode { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
}

public class DocumentSyncDto
{
    public long NodeId { get; set; }
    public long? ParentNodeId { get; set; }
    public NodeType NodeType { get; set; }
    public string EntityCode { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string MaterializedPath { get; set; } = string.Empty;
    public DateTime? DocumentDate { get; set; }
    public string? DocumentNumber { get; set; }
    public string? ExternalDocumentNumber { get; set; }
    public string? SourceType { get; set; }
    public DateTime Timestamp { get; set; }
}
