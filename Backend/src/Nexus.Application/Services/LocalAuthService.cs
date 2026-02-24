using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Nexus.Domain.Entities;

namespace Nexus.Application.Services;

/// <summary>
/// Local autentifikasiya servisi (Email + 2FA)
/// </summary>
public interface ILocalAuthService
{
    Task<RegistrationResult> RegisterAsync(RegisterRequest request);
    Task<LoginResult> LoginAsync(LoginRequest request);
    Task<LoginResult> VerifyTwoFactorAsync(string userId, string token);
    Task<bool> ConfirmEmailAsync(string userId, string token);
    Task<TwoFactorSetupResult> SetupTwoFactorAsync(string userId);
    Task<bool> EnableTwoFactorAsync(string userId, string verificationCode);
    Task<bool> DisableTwoFactorAsync(string userId, string password);
    Task<ForgotPasswordResult> ForgotPasswordAsync(string email);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<bool> ResendConfirmationEmailAsync(string email);
}

public class LocalAuthService : ILocalAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ITwoFactorService _twoFactorService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LocalAuthService> _logger;

    public LocalAuthService(
        IUserRepository userRepository,
        IEmailService emailService,
        ITwoFactorService twoFactorService,
        ITokenService tokenService,
        ILogger<LocalAuthService> logger)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _twoFactorService = twoFactorService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Yeni istifadəçi qeydiyyatı
    /// </summary>
    public async Task<RegistrationResult> RegisterAsync(RegisterRequest request)
    {
        // Email unikallığını yoxla
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new RegistrationResult 
            { 
                Success = false, 
                ErrorMessage = "Bu email ünvanı ilə artıq qeydiyyat mövcuddur" 
            };
        }

        // Username unikallığını yoxla
        existingUser = await _userRepository.GetByUsernameAsync(request.Username);
        if (existingUser != null)
        {
            return new RegistrationResult 
            { 
                Success = false, 
                ErrorMessage = "Bu istifadəçi adı artıq mövcuddur" 
            };
        }

        // Şifrəni hash et
        var passwordHash = HashPassword(request.Password);
        
        // Email təsdiq tokeni yarat
        var emailConfirmationToken = GenerateSecureToken();

        // İstifadəçi yarat
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash,
            DisplayName = request.DisplayName ?? request.Username,
            PhoneNumber = request.PhoneNumber,
            Department = request.Department,
            IsActive = true,
            IsEmailConfirmed = false,
            EmailConfirmationToken = emailConfirmationToken,
            EmailConfirmationTokenExpires = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow,
            OrganizationCode = request.OrganizationCode ?? "default",
            AuthenticationType = AuthenticationType.Local
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        // Email təsdiq məktubu göndər
        await _emailService.SendConfirmationEmailAsync(user.Email, user.Username, emailConfirmationToken, user.UserId);

        _logger.LogInformation("User {Username} registered successfully. Confirmation email sent.", request.Username);

        return new RegistrationResult
        {
            Success = true,
            UserId = user.UserId.ToString(),
            Message = "Qeydiyyat uğurlu oldu. Email ünvanınızı təsdiq edin.",
            RequiresEmailConfirmation = true
        };
    }

    /// <summary>
    /// Giriş (1-ci mərhələ: Email + Şifrə)
    /// </summary>
    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
        {
            return new LoginResult 
            { 
                Success = false, 
                ErrorMessage = "Email və ya şifrə yanlışdır" 
            };
        }

        // Hesab bloklanıbmı?
        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
        {
            return new LoginResult 
            { 
                Success = false, 
                ErrorMessage = $"Hesabınız bloklanıb. {user.LockoutEnd.Value.ToLocalTime():HH:mm} sonra yenidən cəhd edin." 
            };
        }

        // Şifrəni yoxla
        if (!VerifyPassword(request.Password, user.PasswordHash!))
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
            }
            await _userRepository.SaveChangesAsync();

            return new LoginResult 
            { 
                Success = false, 
                ErrorMessage = "Email və ya şifrə yanlışdır" 
            };
        }

        // Email təsdiqlənibmi?
        if (!user.IsEmailConfirmed)
        {
            return new LoginResult 
            { 
                Success = false, 
                ErrorMessage = "Email ünvanınız təsdiqlənməyib. Zəhmət olmasa emailinizi yoxlayın.",
                RequiresEmailConfirmation = true,
                UserId = user.UserId.ToString()
            };
        }

        // Uğursuz cəhdləri sıfırla
        user.AccessFailedCount = 0;
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();

        // 2FA aktivdirsə
        if (user.TwoFactorEnabled)
        {
            return new LoginResult
            {
                Success = true,
                RequiresTwoFactor = true,
                UserId = user.UserId.ToString(),
                TempToken = _tokenService.GenerateTempToken(user.UserId.ToString())
            };
        }

        // JWT token yarat
        var token = _tokenService.GenerateToken(user);

        return new LoginResult
        {
            Success = true,
            Token = token,
            User = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                OrganizationCode = user.OrganizationCode,
                TwoFactorEnabled = user.TwoFactorEnabled
            }
        };
    }

    /// <summary>
    /// 2FA kodunu yoxla (2-ci mərhələ)
    /// </summary>
    public async Task<LoginResult> VerifyTwoFactorAsync(string userId, string token)
    {
        var user = await _userRepository.GetByIdAsync(long.Parse(userId));
        if (user == null || !user.TwoFactorEnabled)
        {
            return new LoginResult { Success = false, ErrorMessage = "İstifadəçi tapılmadı" };
        }

        var isValid = _twoFactorService.ValidateToken(user.TwoFactorSecret!, token);
        if (!isValid)
        {
            return new LoginResult { Success = false, ErrorMessage = "Təsdiq kodu yanlışdır" };
        }

        var jwtToken = _tokenService.GenerateToken(user);

        return new LoginResult
        {
            Success = true,
            Token = jwtToken,
            User = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                OrganizationCode = user.OrganizationCode,
                TwoFactorEnabled = true
            }
        };
    }

    /// <summary>
    /// Email təsdiqi
    /// </summary>
    public async Task<bool> ConfirmEmailAsync(string userId, string token)
    {
        var user = await _userRepository.GetByIdAsync(long.Parse(userId));
        if (user == null) return false;

        if (user.EmailConfirmationToken != token)
            return false;

        if (user.EmailConfirmationTokenExpires < DateTime.UtcNow)
            return false;

        user.IsEmailConfirmed = true;
        user.EmailConfirmationToken = null;
        user.EmailConfirmationTokenExpires = null;
        
        await _userRepository.SaveChangesAsync();
        
        _logger.LogInformation("Email confirmed for user {UserId}", userId);
        return true;
    }

    /// <summary>
    /// 2FA quraşdırması üçün QR kod və manual key
    /// </summary>
    public async Task<TwoFactorSetupResult> SetupTwoFactorAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(long.Parse(userId));
        if (user == null)
            return new TwoFactorSetupResult { Success = false, ErrorMessage = "İstifadəçi tapılmadı" };

        var secret = _twoFactorService.GenerateSecret();
        var qrCodeUri = _twoFactorService.GenerateQrCodeUri(user.Email, secret, "NexusPM");

        // Hələlik aktiv etmə, yalnız göstər
        return new TwoFactorSetupResult
        {
            Success = true,
            Secret = secret,
            QrCodeUri = qrCodeUri,
            ManualEntryKey = _twoFactorService.GetManualEntryKey(secret)
        };
    }

    /// <summary>
    /// 2FA aktiv et
    /// </summary>
    public async Task<bool> EnableTwoFactorAsync(string userId, string verificationCode)
    {
        var user = await _userRepository.GetByIdAsync(long.Parse(userId));
        if (user == null) return false;

        // Sonuncu quraşdırma secret-ni yoxla (session-da saxlanmalıdır)
        // Burada sadələşdirilmiş nümunədir
        
        user.TwoFactorEnabled = true;
        user.TwoFactorSecret = user.TwoFactorSecret; // Session-dan gətirilməlidir
        
        await _userRepository.SaveChangesAsync();
        
        _logger.LogInformation("2FA enabled for user {UserId}", userId);
        return true;
    }

    /// <summary>
    /// 2FA deaktiv et
    /// </summary>
    public async Task<bool> DisableTwoFactorAsync(string userId, string password)
    {
        var user = await _userRepository.GetByIdAsync(long.Parse(userId));
        if (user == null) return false;

        if (!VerifyPassword(password, user.PasswordHash!))
            return false;

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        
        await _userRepository.SaveChangesAsync();
        
        _logger.LogInformation("2FA disabled for user {UserId}", userId);
        return true;
    }

    /// <summary>
    /// Şifrə sıfırlama emaili göndər
    /// </summary>
    public async Task<ForgotPasswordResult> ForgotPasswordAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            // Təhlükəsizlik üçün istifadəçi tapılmadı desək də, uğurlu mesaj göstəririk
            return new ForgotPasswordResult 
            { 
                Success = true, 
                Message = "Əgər email ünvanı mövcuddursa, şifrə sıfırlama linki göndərildi" 
            };
        }

        var resetToken = GenerateSecureToken();
        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(2);
        
        await _userRepository.SaveChangesAsync();
        
        await _emailService.SendPasswordResetEmailAsync(user.Email, user.Username, resetToken, user.UserId);

        return new ForgotPasswordResult 
        { 
            Success = true, 
            Message = "Şifrə sıfırlama linki email ünvanınıza göndərildi" 
        };
    }

    /// <summary>
    /// Şifrəni sıfırla
    /// </summary>
    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(long.Parse(request.UserId));
        if (user == null) return false;

        if (user.PasswordResetToken != request.Token)
            return false;

        if (user.PasswordResetTokenExpires < DateTime.UtcNow)
            return false;

        user.PasswordHash = HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpires = null;
        
        await _userRepository.SaveChangesAsync();
        
        _logger.LogInformation("Password reset for user {UserId}", request.UserId);
        return true;
    }

    /// <summary>
    /// Şifrəni dəyiş
    /// </summary>
    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(long.Parse(userId));
        if (user == null) return false;

        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash!))
            return false;

        user.PasswordHash = HashPassword(request.NewPassword);
        await _userRepository.SaveChangesAsync();
        
        return true;
    }

    /// <summary>
    /// Təsdiq emailini yenidən göndər
    /// </summary>
    public async Task<bool> ResendConfirmationEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || user.IsEmailConfirmed)
            return false;

        var newToken = GenerateSecureToken();
        user.EmailConfirmationToken = newToken;
        user.EmailConfirmationTokenExpires = DateTime.UtcNow.AddHours(24);
        
        await _userRepository.SaveChangesAsync();
        
        await _emailService.SendConfirmationEmailAsync(user.Email, user.Username, newToken, user.UserId);
        
        return true;
    }

    #region Private Methods

    private string HashPassword(string password)
    {
        // Argon2 və ya bcrypt istifadə etmək daha yaxşıdır
        // Burada PBKDF2 nümunəsidir
        byte[] salt = new byte[128 / 8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));

        return $"{Convert.ToBase64String(salt)}.{hashed}";
    }

    private bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var hash = parts[1];

        string computedHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));

        return hash == computedHash;
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

    #endregion
}

#region DTOs

public class RegisterRequest
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public string? OrganizationCode { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class ResetPasswordRequest
{
    public string UserId { get; set; } = null!;
    public string Token { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

public class RegistrationResult
{
    public bool Success { get; set; }
    public string? UserId { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public bool RequiresEmailConfirmation { get; set; }
}

public class LoginResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? TempToken { get; set; }
    public UserDto? User { get; set; }
    public string? ErrorMessage { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public bool RequiresEmailConfirmation { get; set; }
}

public class TwoFactorSetupResult
{
    public bool Success { get; set; }
    public string? Secret { get; set; }
    public string? QrCodeUri { get; set; }
    public string? ManualEntryKey { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UserDto
{
    public long UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string OrganizationCode { get; set; } = null!;
    public bool TwoFactorEnabled { get; set; }
}

#endregion
