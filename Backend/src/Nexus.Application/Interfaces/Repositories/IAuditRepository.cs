namespace Nexus.Application.Interfaces.Repositories;

public interface IAuditRepository
{
    Task LogAsync(object auditEntry);
}
