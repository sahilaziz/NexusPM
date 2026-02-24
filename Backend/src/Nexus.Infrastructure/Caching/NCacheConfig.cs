using Alachisoft.NCache.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nexus.Infrastructure.Caching;

/// <summary>
/// NCache Configuration - Windows-native distributed caching
/// Redis alternative for Microsoft stack
/// </summary>
public static class NCacheConfig
{
    public static IServiceCollection AddNCacheDistributedCache(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Option 1: Use NCache (if installed)
        // Uncomment when NCache is installed on server
        /*
        services.AddNCacheDistributedCache(configuration =>
        {
            configuration.CacheName = configuration.GetValue<string>("NCache:CacheName") ?? "NexusPMCache";
            configuration.EnableClientLogs = configuration.GetValue<bool>("NCache:EnableClientLogs");
            configuration.LogLevel = CacheLogLevel.Error;
        });
        */

        // Option 2: Use SQL Server Distributed Cache (no additional software needed)
        // This is the recommended approach for your current setup
        services.AddDistributedSqlServerCache(options =>
        {
            options.ConnectionString = configuration.GetConnectionString("DefaultConnection")!;
            options.SchemaName = "dbo";
            options.TableName = "CacheStore";
        });

        return services;
    }

    /// <summary>
    /// Initialize SQL Server Cache table
    /// Run this once to create the cache table
    /// </summary>
    public static void InitializeSqlServerCache(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        // SQL to create cache table
        var sql = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CacheStore' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[CacheStore] (
        [Id] NVARCHAR(449) COLLATE SQL_Latin1_General_CP1_CS_AS NOT NULL,
        [Value] VARBINARY(MAX) NOT NULL,
        [ExpiresAtTime] DATETIMEOFFSET NOT NULL,
        [SlidingExpirationInSeconds] BIGINT NULL,
        [AbsoluteExpiration] DATETIMEOFFSET NULL,
        CONSTRAINT [pk_Id] PRIMARY KEY ([Id])
    );

    CREATE NONCLUSTERED INDEX [Index_ExpiresAtTime] 
    ON [dbo].[CacheStore]([ExpiresAtTime]);
END";

        using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        connection.Open();
        using var command = new Microsoft.Data.SqlClient.SqlCommand(sql, connection);
        command.ExecuteNonQuery();
    }
}
