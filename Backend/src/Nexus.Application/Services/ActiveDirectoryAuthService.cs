using System.DirectoryServices.AccountManagement;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Application.Interfaces.Services;
using Nexus.Domain.Entities;

namespace Nexus.Application.Services;

/// <summary>
/// Active Directory autentifikasiya servisi
/// Şifrə sıfırlama dəstəyi ilə (recovery email vasitəsilə)
/// </summary>
public interface IActiveDirectoryAuthService
{
    /// <summary>
    /// AD istifadəçisini autentifikasiya et
    /// </summary>
    Task<ADAuthResult> AuthenticateAsync(string username, string password);
    
    /// <summary>
    /// İlk dəfə login edən AD istifadəçisi üçün profil yarat
    /// </summary>
    Task<User> CreateProfileAsync(ADUserInfo adUserInfo, string organizationCode);
    
    /// <summary>
    /// Recovery email əlavə et (ilk login və ya sonradan)
    /// </summary>
    Task<RecoveryEmailResult> SetRecoveryEmailAsync(long userId, string recoveryEmail);
    
    /// <summary>
    /// Recovery email təsdiqlə
    /// </summary>
    Task<bool> ConfirmRecoveryEmailAsync(long userId, string token);
    
    /// <summary>
    /// Şifrə sıfırlama tələbi (recovery email-ə göndərilir)
    /// </summary>
    Task<ForgotPasswordResult> RequestPasswordResetAsync(string username);
    
    /// <summary>
    /// Şifrəni sıfırla (local backup şifrə)
    /// </summary>
    Task<bool> ResetPasswordAsync(long userId, string token, string newPassword);
    
    /// <summary>
    /// AD şifrəsini dəyiş (Active Directory-də birbaşa)
    /// </summary>
    Task<bool> ChangeADPasswordAsync(string username, string currentPassword, string newPassword);
    
    /// <summary>
    /// Local backup şifrə yarat (AD əlçatan olmadıqda)
    /// </summary>
    Task<bool> SetLocalBackupPasswordAsync(long userId, string password);
    
    /// <summary>
    /// Local backup şifrə ilə giriş (AD offline olduqda)
    /// </summary>
    Task<ADAuthResult> LoginWithBackupPasswordAsync(string username, string password);
}

public class ActiveDirectoryAuthService : IActiveDirectoryAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<ActiveDirectoryAuthService> _logger;
    private readonly ActiveDirectoryAuthConfig _config;

    public ActiveDirectoryAuthService(
        IUserRepository userRepository,
        IEmailService emailService,
        AuthenticationConfig authConfig,
        ILogger<ActiveDirectoryAuthService> logger)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _logger = logger;
        _config = authConfig.ActiveDirectory;
    }

    /// <summary>
    /// AD istifadəçisini autentifikasiya et
    /// </summary>
    public async Task<ADAuthResult> AuthenticateAsync(string username, string password)
    {
        try
        {
            using var context = new PrincipalContext(
                ContextType.Domain, 
                _config.Domain,
                _config.LdapServer);

            // AD-də autentifikasiya
            bool isValid = context.ValidateCredentials(username, password, ContextOptions.SimpleBind);
            
            if (!isValid)
            {
                _logger.LogWarning("AD authentication failed for user {Username}", username);
                return new ADAuthResult 
                { 
                    Success = false, 
                    ErrorMessage = "Yanlış istifadəçi adı və ya şifrə" 
                };
            }

            // İstifadəçi məlumatlarını AD-dən çək
            var userPrincipal = UserPrincipal.FindByIdentity(context, username);
            if (userPrincipal == null)
            {
                return new ADAuthResult 
                { 
                    Success = false, 
                    ErrorMessage = "İstifadəçi tapılmadı" 
                };
            }

            // Qrup üzvlüklərini yoxla
            var groups = GetUserGroups(userPrincipal);
            bool isAdmin = groups.Any(g => _config.AdminGroups.Contains(g, StringComparer.OrdinalIgnoreCase));
            bool isAuthorized = isAdmin || groups.Any(g => _config.UserGroups.Contains(g, StringComparer.OrdinalIgnoreCase));

            if (!isAuthorized)
            {
                return new ADAuthResult 
                { 
                    Success = false, 
                    ErrorMessage = "Sistemin istifadəsinə icazəniz yoxdur" 
                };
            }

            // Database-də istifadəçi varmı?
            var existingUser = await _userRepository.GetByUsernameAsync(username);
            bool isFirstLogin = existingUser == null;

            if (isFirstLogin)
            {
                // İlk login - profil yarat
                _logger.LogInformation("First login for AD user {Username}, creating profile", username);
                return new ADAuthResult
                {
                    Success = true,
                    IsFirstLogin = true,
                    ADUserInfo = new ADUserInfo
                    {
                        Username = username,
                        DisplayName = userPrincipal.DisplayName ?? username,
                        Email = userPrincipal.EmailAddress,
                        GivenName = userPrincipal.GivenName,
                        Surname = userPrincipal.Surname,
                        SID = userPrincipal.Sid?.ToString(),
                        Groups = groups,
                        IsAdmin = isAdmin
                    }
                };
            }

            // Profil tamamlanıbmı?
            bool needsRecoveryEmail = string.IsNullOrEmpty(existingUser.RecoveryEmail) || 
                                      !existingUser.IsRecoveryEmailConfirmed;

            return new ADAuthResult
            {
                Success = true,
                IsFirstLogin = false,
                NeedsRecoveryEmail = needsRecoveryEmail,
                UserId = existingUser.UserId,
                ADUserInfo = new ADUserInfo
                {
                    Username = username,
                    DisplayName = existingUser.DisplayName,
                    Email = existingUser.Email,
                    SID = existingUser.ActiveDirectorySid,
                    Groups = groups,
                    IsAdmin = isAdmin
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AD authentication error for user {Username}", username);
            
            // AD offline ola bilər, backup şifrə ilə yoxlamağa icazə ver
            return new ADAuthResult 
            { 
                Success = false, 
                ErrorMessage = "Autentifikasiya xətası",
                ADUnavailable = true
            };
        }
    }

    /// <summary>
    /// İlk dəfə login edən AD istifadəçisi üçün profil yarat
    /// </summary>
    public async Task<User> CreateProfileAsync(ADUserInfo adUserInfo, string organizationCode)
    {
        var user = new User
        {
            Username = adUserInfo.Username,
            Email = adUserInfo.Email,
            DisplayName = adUserInfo.DisplayName,
            ActiveDirectorySid = adUserInfo.SID,
            Domain = _config.Domain,
            AdGroups = string.Join(",", adUserInfo.Groups),
            Role = adUserInfo.IsAdmin ? UserRole.Admin : UserRole.User,
            OrganizationCode = organizationCode,
            AuthenticationType = AuthenticationType.ActiveDirectory,
            IsProfileCompleted = false, // Recovery email əlavə edilənə qədər
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        _logger.LogInformation("Created profile for AD user {Username} with ID {UserId}", 
            adUserInfo.Username, user.UserId);

        return user;
    }

    /// <summary>
    /// Recovery email əlavə et
    /// </summary>
    public async Task<RecoveryEmailResult> SetRecoveryEmailAsync(long userId, string recoveryEmail)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return new RecoveryEmailResult 
            { 
                Success = false, 
                ErrorMessage = "İstifadəçi tapılmadı" 
            };
        }

        if (user.AuthenticationType != AuthenticationType.ActiveDirectory)
        {
            return new RecoveryEmailResult 
            { 
                Success = false, 
                ErrorMessage = "Yalnız AD istifadəçiləri üçün" 
            };
        }

        // Yeni recovery email üçün token yarat
        var token = GenerateSecureToken();
        
        user.RecoveryEmail = recoveryEmail;
        user.IsRecoveryEmailConfirmed = false;
        user.RecoveryEmailConfirmationToken = token;
        
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        // Təsdiq emaili göndər
        await _emailService.SendConfirmationEmailAsync(
            recoveryEmail, 
            user.DisplayName, 
            token, 
            user.UserId);

        return new RecoveryEmailResult
        {
            Success = true,
            Message = "Recovery email ünvanına təsdiq linki göndərildi"
        };
    }

    /// <summary>
    /// Recovery email təsdiqlə
    /// </summary>
    public async Task<bool> ConfirmRecoveryEmailAsync(long userId, string token)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        if (user.RecoveryEmailConfirmationToken != token)
            return false;

        user.IsRecoveryEmailConfirmed = true;
        user.IsProfileCompleted = true;
        user.RecoveryEmailConfirmationToken = null;
        
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        _logger.LogInformation("Recovery email confirmed for user {UserId}", userId);
        return true;
    }

    /// <summary>
    /// Şifrə sıfırlama tələbi
    /// </summary>
    public async Task<ForgotPasswordResult> RequestPasswordResetAsync(string username)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null || user.AuthenticationType != AuthenticationType.ActiveDirectory)
        {
            // Təhlükəsizlik üçün həmişə uğurlu mesaj göstər
            return new ForgotPasswordResult 
            { 
                Success = true, 
                Message = "Əgər istifadəçi mövcuddursa, recovery email-ə link göndərildi" 
            };
        }

        if (string.IsNullOrEmpty(user.RecoveryEmail) || !user.IsRecoveryEmailConfirmed)
        {
            return new ForgotPasswordResult 
            { 
                Success = false, 
                ErrorMessage = "Recovery email təyin edilməyib. Zəhmət olmasa administrator ilə əlaqə saxlayın." 
            };
        }

        // Token yarat
        var token = GenerateSecureToken();
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(2);
        
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        // Recovery email-ə göndər
        await _emailService.SendPasswordResetEmailAsync(
            user.RecoveryEmail,
            user.DisplayName,
            token,
            user.UserId);

        _logger.LogInformation("Password reset requested for AD user {Username}", username);

        return new ForgotPasswordResult 
        { 
            Success = true, 
            Message = "Şifrə sıfırlama linki recovery email ünvanınıza göndərildi" 
        };
    }

    /// <summary>
    /// Şifrəni sıfırla (local backup şifrə)
    /// </summary>
    public async Task<bool> ResetPasswordAsync(long userId, string token, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        if (user.PasswordResetToken != token)
            return false;

        if (user.PasswordResetTokenExpires < DateTime.UtcNow)
            return false;

        // Local backup şifrə yarat
        user.PasswordHash = HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpires = null;
        
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        _logger.LogInformation("Password reset completed for user {UserId}", userId);
        return true;
    }

    /// <summary>
    /// AD şifrəsini dəyiş (birbaşa AD-də)
    /// </summary>
    public async Task<bool> ChangeADPasswordAsync(string username, string currentPassword, string newPassword)
    {
        try
        {
            using var context = new PrincipalContext(ContextType.Domain, _config.Domain);
            
            var userPrincipal = UserPrincipal.FindByIdentity(context, username);
            if (userPrincipal == null) return false;

            // AD şifrəsini dəyiş
            userPrincipal.ChangePassword(currentPassword, newPassword);
            userPrincipal.Save();

            _logger.LogInformation("AD password changed for user {Username}", username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change AD password for {Username}", username);
            return false;
        }
    }

    /// <summary>
    /// Local backup şifrə yarat
    /// </summary>
    public async Task<bool> SetLocalBackupPasswordAsync(long userId, string password)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.AuthenticationType != AuthenticationType.ActiveDirectory)
            return false;

        user.PasswordHash = HashPassword(password);
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Local backup şifrə ilə giriş (AD offline olduqda)
    /// </summary>
    public async Task<ADAuthResult> LoginWithBackupPasswordAsync(string username, string password)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null || user.AuthenticationType != AuthenticationType.ActiveDirectory)
        {
            return new ADAuthResult 
            { 
                Success = false, 
                ErrorMessage = "İstifadəçi tapılmadı" 
            };
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            return new ADAuthResult 
            { 
                Success = false, 
                ErrorMessage = "Backup şifrə təyin edilməyib" 
            };
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            return new ADAuthResult 
            { 
                Success = false, 
                ErrorMessage = "Yanlış şifrə" 
            };
        }

        return new ADAuthResult
        {
            Success = true,
            UserId = user.UserId,
            IsBackupLogin = true,
            ADUserInfo = new ADUserInfo
            {
                Username = user.Username,
                DisplayName = user.DisplayName,
                Email = user.Email,
                IsAdmin = user.Role == UserRole.Admin
            }
        };
    }

    #region Private Methods

    private List<string> GetUserGroups(UserPrincipal userPrincipal)
    {
        var groups = new List<string>();
        
        if (userPrincipal.GetGroups() != null)
        {
            foreach (var group in userPrincipal.GetGroups())
            {
                groups.Add(group.Name);
            }
        }

        return groups;
    }

    private string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private string HashPassword(string password)
    {
        // PBKDF2 hashing
        byte[] salt = new byte[128 / 8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        string hashed = Convert.ToBase64String(System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: 100000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 256 / 8));

        return $"{Convert.ToBase64String(salt)}.{hashed}";
    }

    private bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var hash = parts[1];

        string computedHash = Convert.ToBase64String(System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: 100000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 256 / 8));

        return hash == computedHash;
    }

    #endregion
}

#region DTOs

public class ADAuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsFirstLogin { get; set; }
    public bool NeedsRecoveryEmail { get; set; }
    public bool IsBackupLogin { get; set; }
    public bool ADUnavailable { get; set; }
    public long? UserId { get; set; }
    public ADUserInfo? ADUserInfo { get; set; }
}

public class ADUserInfo
{
    public string Username { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Email { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? SID { get; set; }
    public List<string> Groups { get; set; } = new();
    public bool IsAdmin { get; set; }
}

public class RecoveryEmailResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ForgotPasswordResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion
