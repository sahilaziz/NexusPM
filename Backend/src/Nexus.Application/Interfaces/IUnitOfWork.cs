namespace Nexus.Application.Interfaces;

/// <summary>
/// Unit of Work pattern interface
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync();
}
