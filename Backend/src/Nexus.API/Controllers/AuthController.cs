using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.API.Auth;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Application.Services;
using System.Security.Claims;

namespace Nexus.API.Controllers;

/// <summary>
/// Autentifikasiya controller-i
/// Supports: Local (Email+2FA) and Active Directory modes
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILocalAuthService? _localAuthService;
    private readonly IActiveDirectoryAuthService? _adAuthService;
    private readonly AuthenticationConfig _authConfig;
    private readonly IUserRepository _userRepository;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AuthenticationConfig authConfig,
        IUserRepository userRepository,
        JwtService jwtService,
        ILogger<AuthController> logger,
        ILocalAuthService? localAuthService = null,
        IActiveDirectoryAuthService? adAuthService = null)
    {
        _authConfig = authConfig;
        _userRepository = userRepository;
        _jwtService = jwtService;
        _logger = logger;
        _localAuthService = localAuthService;
        _adAuthService = adAuthService;
    }

    /// <summary>
    /// Sistemdəki autentifikasiya rejimini qaytarır
    /// </summary>
    [HttpGet("mode")]
    [AllowAnonymous]
    public IActionResult GetAuthMode()
    {
        return Ok(new
        {
            Mode = _authConfig.Mode.ToString(),
            Features = new
            {
                LocalAuth = _authConfig.Mode == AuthenticationMode.Local || _authConfig.Mode == AuthenticationMode.Mixed,
                ActiveDirectory = _authConfig.Mode == AuthenticationMode.ActiveDirectory || _authConfig.Mode == AuthenticationMode.Mixed,
                TwoFactor = _authConfig.Local?.EnableTwoFactor ?? false,
                EmailConfirmation = _authConfig.Local?.RequireEmailConfirmation ?? false,
                ADPasswordReset = _authConfig.Mode != AuthenticationMode.Local // AD users can reset password via recovery email
            }
        });
    }

    #region Local Authentication Endpoints

    /// <summary>
    /// Yeni istifadəçi qeydiyyatı (Local mode only)
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (_authConfig.Mode == AuthenticationMode.ActiveDirectory)
        {
            return BadRequest(new { Error = "Local registration is disabled. Use Active Directory authentication." });
        }

        if (_localAuthService == null)
        {
            return StatusCode(500, new { Error = "Local authentication service not available" });
        }

        var result = await _localAuthService.RegisterAsync(request);
        
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }

        return Ok(new
        {
            Message = result.Message,
            UserId = result.UserId,
            RequiresEmailConfirmation = result.RequiresEmailConfirmation
        });
    }

    /// <summary>
    /// Email təsdiqi (Local mode only)
    /// </summary>
    [HttpGet("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromQuery] long userId, [FromQuery] string token)
    {
        if (_localAuthService == null)
        {
            return StatusCode(500, new { Error = "Local authentication service not available" });
        }

        var success = await _localAuthService.ConfirmEmailAsync(userId.ToString(), token);
        
        if (!success)
        {
            return BadRequest(new { Error = "Invalid or expired confirmation token" });
        }

        return Ok(new { Message = "Email confirmed successfully. You can now log in." });
    }

    /// <summary>
    /// Giriş (Local mode - Step 1: Email + Password)
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (_authConfig.Mode == AuthenticationMode.ActiveDirectory)
        {
            return BadRequest(new { Error = "Local login is disabled. Use Active Directory authentication." });
        }

        if (_localAuthService == null)
        {
            return StatusCode(500, new { Error = "Local authentication service not available" });
        }

        var result = await _localAuthService.LoginAsync(request);
        
        if (!result.Success)
        {
            if (result.RequiresEmailConfirmation)
            {
                return BadRequest(new 
                { 
                    Error = result.ErrorMessage,
                    RequiresEmailConfirmation = true,
                    UserId = result.UserId
                });
            }

            return Unauthorized(new { Error = result.ErrorMessage });
        }

        // If 2FA is required
        if (result.RequiresTwoFactor)
        {
            return Ok(new
            {
                RequiresTwoFactor = true,
                TempToken = result.TempToken,
                Message = "Please enter your 2FA code"
            });
        }

        return Ok(new
        {
            Token = result.Token,
            User = result.User,
            Message = "Login successful"
        });
    }

    /// <summary>
    /// 2FA kodunu təsdiqlə (Local mode - Step 2)
    /// </summary>
    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
    {
        if (_localAuthService == null)
        {
            return StatusCode(500, new { Error = "Local authentication service not available" });
        }

        var result = await _localAuthService.VerifyTwoFactorAsync(request.UserId, request.Code);
        
        if (!result.Success)
        {
            return Unauthorized(new { Error = result.ErrorMessage });
        }

        return Ok(new
        {
            Token = result.Token,
            User = result.User,
            Message = "Login successful"
        });
    }

    #endregion

    #region Active Directory Endpoints

    /// <summary>
    /// Active Directory ilə giriş
    /// </summary>
    [HttpPost("ad-login")]
    [AllowAnonymous]
    public async Task<IActionResult> ActiveDirectoryLogin([FromBody] ADLoginRequest request)
    {
        if (_authConfig.Mode == AuthenticationMode.Local)
        {
            return BadRequest(new { Error = "Active Directory authentication is disabled" });
        }

        if (_adAuthService == null)
        {
            return StatusCode(500, new { Error = "Active Directory service not available" });
        }

        var result = await _adAuthService.AuthenticateAsync(request.Username, request.Password);
        
        if (!result.Success)
        {
            // AD offline ola bilər, backup şifrə ilə cəhd et
            if (result.ADUnavailable)
            {
                var backupResult = await _adAuthService.LoginWithBackupPasswordAsync(request.Username, request.Password);
                if (backupResult.Success)
                {
                    var token = GenerateJwtToken(backupResult.ADUserInfo!, backupResult.UserId!.Value);
                    return Ok(new
                    {
                        Token = token,
                        User = backupResult.ADUserInfo,
                        IsBackupLogin = true,
                        Message = "Logged in with backup password (AD unavailable)"
                    });
                }
            }

            return Unauthorized(new { Error = result.ErrorMessage });
        }

        // İlk login - profil yaratmaq lazımdır
        if (result.IsFirstLogin)
        {
            return Ok(new
            {
                IsFirstLogin = true,
                ADUserInfo = result.ADUserInfo,
                Message = "First login. Please complete your profile by setting a recovery email."
            });
        }

        // Recovery email lazımdırmı?
        if (result.NeedsRecoveryEmail)
        {
            return Ok(new
            {
                NeedsRecoveryEmail = true,
                UserId = result.UserId,
                Message = "Please set a recovery email for password reset functionality."
            });
        }

        // Normal giriş
        var jwtToken = GenerateJwtToken(result.ADUserInfo!, result.UserId!.Value);

        return Ok(new
        {
            Token = jwtToken,
            User = result.ADUserInfo,
            Message = "Login successful"
        });
    }

    /// <summary>
    /// İlk login üçün profil yarat (AD istifadəçiləri üçün)
    /// </summary>
    [HttpPost("ad-complete-profile")]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteADProfile([FromBody] CompleteADProfileRequest request)
    {
        if (_adAuthService == null)
        {
            return StatusCode(500, new { Error = "Active Directory service not available" });
        }

        // Profil yarat
        var user = await _adAuthService.CreateProfileAsync(request.ADUserInfo, request.OrganizationCode);
        
        // Recovery email əlavə et
        if (!string.IsNullOrEmpty(request.RecoveryEmail))
        {
            var recoveryResult = await _adAuthService.SetRecoveryEmailAsync(user.UserId, request.RecoveryEmail);
            if (!recoveryResult.Success)
            {
                return BadRequest(new { Error = recoveryResult.ErrorMessage });
            }

            return Ok(new
            {
                Message = "Profile created. Please check your recovery email to confirm.",
                UserId = user.UserId,
                RequiresEmailConfirmation = true
            });
        }

        // Recovery email olmadan
        var token = GenerateJwtToken(request.ADUserInfo, user.UserId);
        
        return Ok(new
        {
            Token = token,
            Message = "Profile created. Please add a recovery email for password reset functionality."
        });
    }

    /// <summary>
    /// Recovery email təsdiqi (AD istifadəçiləri üçün)
    /// </summary>
    [HttpGet("ad-confirm-recovery-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmRecoveryEmail([FromQuery] long userId, [FromQuery] string token)
    {
        if (_adAuthService == null)
        {
            return StatusCode(500, new { Error = "Active Directory service not available" });
        }

        var success = await _adAuthService.ConfirmRecoveryEmailAsync(userId, token);
        
        if (!success)
        {
            return BadRequest(new { Error = "Invalid or expired confirmation token" });
        }

        return Ok(new { Message = "Recovery email confirmed successfully. You can now use password reset functionality." });
    }

    /// <summary>
    /// Recovery email əlavə et (mövcud AD istifadəçiləri üçün)
    /// </summary>
    [HttpPost("ad-set-recovery-email")]
    [Authorize]
    public async Task<IActionResult> SetRecoveryEmail([FromBody] SetRecoveryEmailRequest request)
    {
        if (_adAuthService == null)
        {
            return StatusCode(500, new { Error = "Active Directory service not available" });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await _adAuthService.SetRecoveryEmailAsync(userId, request.RecoveryEmail);
        
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }

        return Ok(new { Message = result.Message });
    }

    /// <summary>
    /// AD istifadəçisi üçün şifrə sıfırlama tələbi
    /// </summary>
    [HttpPost("ad-forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ADForgotPassword([FromBody] ADForgotPasswordRequest request)
    {
        if (_authConfig.Mode == AuthenticationMode.Local)
        {
            return BadRequest(new { Error = "Active Directory authentication is disabled" });
        }

        if (_adAuthService == null)
        {
            return StatusCode(500, new { Error = "Active Directory service not available" });
        }

        var result = await _adAuthService.RequestPasswordResetAsync(request.Username);
        
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }

        return Ok(new { Message = result.Message });
    }

    /// <summary>
    /// AD istifadəçisi üçün şifrəni sıfırla (local backup şifrə)
    /// </summary>
    [HttpPost("ad-reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ADResetPassword([FromBody] ADResetPasswordRequest request)
    {
        if (_adAuthService == null)
        {
            return StatusCode(500, new { Error = "Active Directory service not available" });
        }

        var success = await _adAuthService.ResetPasswordAsync(request.UserId, request.Token, request.NewPassword);
        
        if (!success)
        {
            return BadRequest(new { Error = "Invalid or expired reset token" });
        }

        return Ok(new { Message = "Password reset successful. You can now log in with your new backup password." });
    }

    /// <summary>
    /// AD şifrəsini birbaşa dəyiş (carı şifrə ilə)
    /// </summary>
    [HttpPost("ad-change-password")]
    [Authorize]
    public async Task<IActionResult> ADChangePassword([FromBody] ADChangePasswordRequest request)
    {
        if (_adAuthService == null)
        {
            return StatusCode(500, new { Error = "Active Directory service not available" });
        }

        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized();
        }

        var success = await _adAuthService.ChangeADPasswordAsync(username, request.CurrentPassword, request.NewPassword);
        
        if (!success)
        {
            return BadRequest(new { Error = "Failed to change password. Please check your current password." });
        }

        return Ok(new { Message = "Password changed successfully" });
    }

    /// <summary>
    /// Local backup şifrə yarat (AD istifadəçiləri üçün)
    /// </summary>
    [HttpPost("ad-set-backup-password")]
    [Authorize]
    public async Task<IActionResult> SetBackupPassword([FromBody] SetBackupPasswordRequest request)
    {
        if (_adAuthService == null)
        {
            return StatusCode(500, new { Error = "Active Directory service not available" });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var success = await _adAuthService.SetLocalBackupPasswordAsync(userId, request.Password);
        
        if (!success)
        {
            return BadRequest(new { Error = "Failed to set backup password" });
        }

        return Ok(new { Message = "Backup password set successfully. You can use this when AD is unavailable." });
    }

    #endregion

    #region Common Endpoints

    /// <summary>
    /// Cari istifadəçi məlumatları
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null)
        {
            return NotFound(new { Error = "User not found" });
        }

        return Ok(new
        {
            user.UserId,
            user.Username,
            user.Email,
            user.RecoveryEmail,
            user.IsRecoveryEmailConfirmed,
            user.DisplayName,
            user.PhoneNumber,
            user.Department,
            user.Position,
            user.OrganizationCode,
            user.Role,
            user.AuthenticationType,
            user.TwoFactorEnabled,
            user.IsEmailConfirmed,
            user.IsProfileCompleted,
            user.LastLoginAt,
            user.CreatedAt
        });
    }

    /// <summary>
    /// Şifrəni dəyiş (Local istifadəçilər üçün)
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (_localAuthService == null)
        {
            return StatusCode(500, new { Error = "Local authentication service not available" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var success = await _localAuthService.ChangePasswordAsync(userId, request);
        
        if (!success)
        {
            return BadRequest(new { Error = "Current password is incorrect" });
        }

        return Ok(new { Message = "Password changed successfully" });
    }

    /// <summary>
    /// Email təsdiqini yenidən göndər
    /// </summary>
    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request)
    {
        if (_localAuthService == null)
        {
            return StatusCode(500, new { Error = "Local authentication service not available" });
        }

        await _localAuthService.ResendConfirmationEmailAsync(request.Email);
        
        return Ok(new { Message = "If the email exists, a confirmation link has been sent" });
    }

    #endregion

    #region Private Methods

    private string GenerateJwtToken(ADUserInfo userInfo, long userId)
    {
        return _jwtService.GenerateToken(
            userId.ToString(),
            userInfo.Username,
            userInfo.IsAdmin ? "Admin" : "User",
            "default"
        );
    }

    #endregion
}

// Request DTOs
public class VerifyTwoFactorRequest
{
    public string UserId { get; set; } = null!;
    public string Code { get; set; } = null!;
}

public class ResendConfirmationRequest
{
    public string Email { get; set; } = null!;
}

// AD Request DTOs
public class ADLoginRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class CompleteADProfileRequest
{
    public ADUserInfo ADUserInfo { get; set; } = null!;
    public string OrganizationCode { get; set; } = "default";
    public string? RecoveryEmail { get; set; }
}

public class SetRecoveryEmailRequest
{
    public string RecoveryEmail { get; set; } = null!;
}

public class ADForgotPasswordRequest
{
    public string Username { get; set; } = null!;
}

public class ADResetPasswordRequest
{
    public long UserId { get; set; }
    public string Token { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

public class ADChangePasswordRequest
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

public class SetBackupPasswordRequest
{
    public string Password { get; set; } = null!;
}
