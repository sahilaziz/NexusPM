using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(string userId);
    Task CreateAsync(User user);
    Task UpdateAsync(User user);
}
