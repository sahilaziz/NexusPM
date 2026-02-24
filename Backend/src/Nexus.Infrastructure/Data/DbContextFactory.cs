using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Nexus.Application.Interfaces;

namespace Nexus.Infrastructure.Data;

/// <summary>
/// Read/Write separation üçün DbContext Factory
/// SQL Server Always On Read Replicas dəstəyi
/// </summary>
public interface IDbContextFactory
{
    AppDbContext CreateWriteContext();
    AppDbContext CreateReadContext();
}

public class DbContextFactory : IDbContextFactory
{
    private readonly IConfiguration _configuration;
    private readonly DbContextOptions<AppDbContext> _writeOptions;
    private readonly DbContextOptions<AppDbContext> _readOptions;

    public DbContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
        
        // Write context (Primary)
        var writeConnection = _configuration.GetConnectionString("DefaultConnection");
        _writeOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(writeConnection, options =>
            {
                options.EnableRetryOnFailure(3);
                options.MaxRetryDelay(TimeSpan.FromSeconds(30));
            })
            .Options;

        // Read context (Replica)
        var readConnection = _configuration.GetConnectionString("ReadReplicaConnection") 
            ?? writeConnection;
        _readOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(readConnection, options =>
            {
                options.EnableRetryOnFailure(3);
            })
            .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
            .Options;
    }

    public AppDbContext CreateWriteContext()
    {
        return new AppDbContext(_writeOptions);
    }

    public AppDbContext CreateReadContext()
    {
        return new AppDbContext(_readOptions);
    }
}

/// <summary>
/// Unit of Work pattern with Read/Write separation
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(IDbContextFactory factory, bool isReadOnly = false)
    {
        _context = isReadOnly ? factory.CreateReadContext() : factory.CreateWriteContext();
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
