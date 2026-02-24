namespace Nexus.Application.Interfaces.Services;

public interface ITwoFactorService
{
    Task<string> GenerateCodeAsync(string userId);
    Task<bool> ValidateCodeAsync(string userId, string code);
}
