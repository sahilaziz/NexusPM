using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long userId);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> ValidateCredentialsAsync(string username, string password);
    Task<IEnumerable<User>> GetByOrganizationAsync(string organizationCode);
    Task AddAsync(User user);
    Task<User> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task UpdateLastLoginAsync(int userId);
    Task SaveChangesAsync();
    Task<bool> IsUsernameAvailableAsync(string username);
    Task<bool> IsEmailAvailableAsync(string email);
}
