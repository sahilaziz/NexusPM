namespace Nexus.Application.Interfaces.Services;

public interface ITokenService
{
    string GenerateToken(string userId, string email);
}
