using System.Security.Cryptography;
using Nexus.Application.Interfaces.Services;
using OtpNet;

namespace Nexus.Infrastructure.Services;

/// <summary>
/// TOTP (Time-based One-Time Password) 2FA servisi
/// </summary>
public class TwoFactorService : ITwoFactorService
{
    /// <summary>
    /// Yeni 2FA secret key yaradır
    /// </summary>
    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    /// <summary>
    /// QR kod URI-si yaradır (Google Authenticator, Microsoft Authenticator üçün)
    /// </summary>
    public string GenerateQrCodeUri(string email, string secret, string issuer)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.ProvisioningUri(email, issuer);
    }

    /// <summary>
    /// Manual daxil etmə üçün key (boşluqsuz)
    /// </summary>
    public string GetManualEntryKey(string secret)
    {
        return secret.ToUpperInvariant();
    }

    /// <summary>
    /// TOTP kodunu yoxlayır
    /// </summary>
    public bool ValidateToken(string secret, string token)
    {
        try
        {
            var totp = new Totp(Base32Encoding.ToBytes(secret));
            return totp.VerifyTotp(token, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// JWT Token servisi
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        // JWT token yaratmaq üçün mövcud JwtService-dən istifadə edin
        // Burada sadəcə stub implementasiyadır
        var jwtService = new JwtService(_configuration);
        
        // Map User to JWT model
        return jwtService.GenerateToken(
            user.UserId.ToString(), 
            user.Username, 
            user.Role.ToString(),
            user.OrganizationCode
        );
    }

    public string GenerateTempToken(string userId)
    {
        // 2FA müddətində istifadə olunan temporary token
        var key = System.Text.Encoding.UTF8.GetBytes(
            _configuration["Jwt:SecretKey"]!);
        
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("sub", userId),
                new System.Security.Claims.Claim("type", "temp_2fa")
            }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string? ValidateToken(string token)
    {
        try
        {
            var key = System.Text.Encoding.UTF8.GetBytes(
                _configuration["Jwt:SecretKey"]!);
            
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var validationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal.FindFirst("sub")?.Value;
        }
        catch
        {
            return null;
        }
    }
}
