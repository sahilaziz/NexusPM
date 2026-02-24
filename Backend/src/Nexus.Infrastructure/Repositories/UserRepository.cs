using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Data;

namespace Nexus.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(long userId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> ValidateCredentialsAsync(string username, string password)
    {
        var user = await GetByUsernameAsync(username);
        
        if (user == null || user.PasswordHash == null) return null;

        // Verify password hash
        if (!VerifyPasswordHash(password, user.PasswordHash))
            return null;

        return user;
    }

    public async Task<IEnumerable<User>> GetByOrganizationAsync(string organizationCode)
    {
        return await _context.Users
            .Where(u => u.OrganizationCode == organizationCode && u.IsActive)
            .OrderBy(u => u.DisplayName)
            .ToListAsync();
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task<User> CreateAsync(User user)
    {
        user.CreatedAt = DateTime.UtcNow;
        
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        
        return user;
    }

    public Task UpdateAsync(User user)
    {
        user.ModifiedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        var user = await _context.Users.FindAsync((long)userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsUsernameAvailableAsync(string username)
    {
        return !await _context.Users.AnyAsync(u => u.Username == username);
    }

    public async Task<bool> IsEmailAvailableAsync(string email)
    {
        return !await _context.Users.AnyAsync(u => u.Email == email);
    }

    // Simple password verification - actual hashing done in service
    private bool VerifyPasswordHash(string password, string storedHash)
    {
        // This is just a placeholder - actual hashing is done in LocalAuthService
        return false;
    }
}
