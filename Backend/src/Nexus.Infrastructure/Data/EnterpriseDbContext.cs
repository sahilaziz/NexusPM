using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Data.SqlClient;

namespace Nexus.Infrastructure.Data;

/// <summary>
/// Enterprise-grade database optimizations for 5000+ users
/// </summary>
public class EnterpriseDbContext : AppDbContext
{
    private readonly ILogger<EnterpriseDbContext> _logger;
    private readonly IConfiguration _configuration;

    public EnterpriseDbContext(
        DbContextOptions<AppDbContext> options,
        ILogger<EnterpriseDbContext> logger,
        IConfiguration configuration) : base(options)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Connection resiliency (retry on transient failures)
        optionsBuilder.EnableSensitiveDataLogging(false);
        optionsBuilder.EnableDetailedErrors(false);
        
        // Command timeout: 30 seconds for queries, 5 minutes for migrations
        optionsBuilder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        
        // Add performance interceptors
        optionsBuilder.AddInterceptors(new CommandTimeoutInterceptor());
        optionsBuilder.AddInterceptors(new QueryPerformanceInterceptor(_logger));
        
        // Connection pooling optimization
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            MaxPoolSize = 200,          // Default: 100
            MinPoolSize = 10,           // Default: 0
            ConnectTimeout = 30,        // 30 seconds
            CommandTimeout = 30,        // 30 seconds
            MultipleActiveResultSets = true,
            ApplicationName = "NexusPM",
            Encrypt = true,
            TrustServerCertificate = true
        };
        
        optionsBuilder.UseSqlServer(builder.ConnectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
                
            sqlOptions.MinBatchSize(1);
            sqlOptions.MaxBatchSize(100);
            sqlOptions.MigrationsAssembly("Nexus.Infrastructure");
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Add query filters for soft delete (performance optimization)
        modelBuilder.Entity<DocumentNode>()
            .HasQueryFilter(d => !d.IsDeleted);

        // Add computed columns for performance
        modelBuilder.Entity<DocumentNode>()
            .Property(e => e.MaterializedPath)
            .HasComputedColumnSql(
                "CASE WHEN ParentNodeId IS NULL THEN '/1/' ELSE CONCAT(Parent.MaterializedPath, NodeId, '/') END",
                stored: true);

        // Partitioning support (for SQL Server Enterprise)
        // Note: Actual partition scheme creation requires DDL scripts
        
        // Add indexes for enterprise workloads
        AddEnterpriseIndexes(modelBuilder);
    }

    private void AddEnterpriseIndexes(ModelBuilder modelBuilder)
    {
        // Covering index for search queries
        modelBuilder.Entity<DocumentNode>()
            .HasIndex(d => new { d.EntityCode, d.EntityName, d.DocumentDate })
            .IncludeProperties(d => new { d.NodeType, d.MaterializedPath, d.NormalizedDocumentNumber })
            .HasFilter("[IsDeleted] = 0 AND [NodeType] = 'DOCUMENT'")
            .HasDatabaseName("IX_DocumentNodes_Search_Covering");

        // Full-text search index
        modelBuilder.Entity<DocumentNode>()
            .HasIndex("EntityName", "DocumentSubject")
            .IsFullText()
            .HasDatabaseName("FT_DocumentNodes_Content");

        // Partition-aligned index for large tables
        modelBuilder.Entity<SyncQueue>()
            .HasIndex(s => new { s.Status, s.CreatedAt })
            .HasDatabaseName("IX_SyncQueue_Status_Created");

        // Hash index for lookup (SQL 2022+)
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("IX_Users_Username_Unique");
    }
}

/// <summary>
/// Query performance interceptor
/// </summary>
public class QueryPerformanceInterceptor : DbCommandInterceptor
{
    private readonly ILogger _logger;
    private readonly TimeSpan _slowQueryThreshold = TimeSpan.FromSeconds(1);

    public QueryPerformanceInterceptor(ILogger logger)
    {
        _logger = logger;
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        var duration = eventData.Duration;
        
        if (duration > _slowQueryThreshold)
        {
            _logger.LogWarning(
                "Slow query detected: {Duration}ms\n{Command}",
                duration.TotalMilliseconds,
                command.CommandText);
        }

        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}

/// <summary>
/// Command timeout interceptor
/// </summary>
public class CommandTimeoutInterceptor : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        // Increase timeout for complex queries
        if (command.CommandText.Contains("SEARCH", StringComparison.OrdinalIgnoreCase))
        {
            command.CommandTimeout = 60; // 60 seconds for search
        }
        
        return result;
    }
}

/// <summary>
/// Read/Write splitting for SQL Server replicas
/// </summary>
public class ReadWriteSplittingInterceptor : DbCommandInterceptor
{
    private readonly string _readOnlyConnectionString;

    public ReadWriteSplittingInterceptor(IConfiguration configuration)
    {
        _readOnlyConnectionString = configuration.GetConnectionString("ReadOnlyConnection") 
            ?? configuration.GetConnectionString("DefaultConnection");
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        // Route SELECT queries to read replica
        if (IsReadOnlyQuery(command.CommandText))
        {
            if (command.Connection is SqlConnection sqlConnection)
            {
                // Note: In production, use a proper connection factory
                // This is a simplified example
                sqlConnection.ConnectionString = _readOnlyConnectionString;
            }
        }

        return result;
    }

    private bool IsReadOnlyQuery(string sql)
    {
        var trimmed = sql.Trim().ToUpperInvariant();
        return trimmed.StartsWith("SELECT") && 
               !trimmed.Contains("INSERT") && 
               !trimmed.Contains("UPDATE") && 
               !trimmed.Contains("DELETE");
    }
}

/// <summary>
/// Database performance utilities
/// </summary>
public static class DatabasePerformanceExtensions
{
    /// <summary>
    /// Add compiled queries for hot paths
    /// </summary>
    private static readonly Func<AppDbContext, long, Task<DocumentNode?>> GetDocumentByIdQuery =
        EF.CompileQuery((AppDbContext context, long id) =>
            context.DocumentNodes
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.NodeId == id && !d.IsDeleted));

    private static readonly Func<AppDbContext, string, IAsyncEnumerable<DocumentNode>> SearchDocumentsQuery =
        EF.CompileQuery((AppDbContext context, string searchTerm) =>
            context.DocumentNodes
                .AsNoTracking()
                .Where(d => !d.IsDeleted && 
                    (d.EntityName.Contains(searchTerm) || 
                     d.NormalizedDocumentNumber!.Contains(searchTerm))));

    public static Task<DocumentNode?> GetDocumentByIdFastAsync(this AppDbContext context, long id)
    {
        return GetDocumentByIdQuery(context, id);
    }

    public static IAsyncEnumerable<DocumentNode> SearchDocumentsFast(this AppDbContext context, string searchTerm)
    {
        return SearchDocumentsQuery(context, searchTerm);
    }

    /// <summary>
    /// Bulk insert extension for high-volume operations
    /// </summary>
    public static async Task BulkInsertAsync<T>(this AppDbContext context, 
        IEnumerable<T> entities, 
        int batchSize = 1000) where T : class
    {
        var entityList = entities.ToList();
        var total = entityList.Count;
        var processed = 0;

        while (processed < total)
        {
            var batch = entityList.Skip(processed).Take(batchSize).ToList();
            await context.Set<T>().AddRangeAsync(batch);
            await context.SaveChangesAsync();
            
            processed += batch.Count;
            
            // Clear change tracker to free memory
            context.ChangeTracker.Clear();
        }
    }
}
