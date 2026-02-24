namespace Nexus.Domain.Entities;

/// <summary>
/// Sistem istifadəçisi (Local və ya Active Directory)
/// </summary>
public class User
{
    public long UserId { get; set; }
    
    /// <summary>
    /// İstifadəçi adı (login üçün)
    /// </summary>
    public string Username { get; set; } = null!;
    
    /// <summary>
    /// Email ünvanı (iş/şirkət emaili)
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// Recovery email (şifrə sıfırlama üçün - şəxsi və ya alternativ email)
    /// </summary>
    public string? RecoveryEmail { get; set; }
    
    /// <summary>
    /// Recovery email təsdiqlənibmi?
    /// </summary>
    public bool IsRecoveryEmailConfirmed { get; set; } = false;
    
    /// <summary>
    /// Recovery email təsdiq tokeni
    /// </summary>
    public string? RecoveryEmailConfirmationToken { get; set; }
    
    /// <summary>
    /// Tam ad (Ad Soyad)
    /// </summary>
    public string DisplayName { get; set; } = null!;
    
    /// <summary>
    /// Telefon nömrəsi
    /// </summary>
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// Şöbə/Bölmə
    /// </summary>
    public string? Department { get; set; }
    
    /// <summary>
    /// Vəzifə
    /// </summary>
    public string? Position { get; set; }
    
    /// <summary>
    /// Təşkilat kodu
    /// </summary>
    public string OrganizationCode { get; set; } = "default";
    
    #region Local Authentication
    
    /// <summary>
    /// Şifrə hash-i (yalnız Local istifadəçilər üçün)
    /// </summary>
    public string? PasswordHash { get; set; }
    
    /// <summary>
    /// Email təsdiqlənibmi?
    /// </summary>
    public bool IsEmailConfirmed { get; set; } = false;
    
    /// <summary>
    /// Email təsdiq tokeni
    /// </summary>
    public string? EmailConfirmationToken { get; set; }
    
    /// <summary>
    /// Email təsdiq tokeninin bitmə tarixi
    /// </summary>
    public DateTime? EmailConfirmationTokenExpires { get; set; }
    
    /// <summary>
    /// 2FA aktivdir?
    /// </summary>
    public bool TwoFactorEnabled { get; set; } = false;
    
    /// <summary>
    /// 2FA secret key (TOTP)
    /// </summary>
    public string? TwoFactorSecret { get; set; }
    
    /// <summary>
    /// Şifrə sıfırlama tokeni
    /// </summary>
    public string? PasswordResetToken { get; set; }
    
    /// <summary>
    /// Şifrə sıfırlama tokeninin bitmə tarixi
    /// </summary>
    public DateTime? PasswordResetTokenExpires { get; set; }
    
    /// <summary>
    /// Uğursuz giriş cəhdləri
    /// </summary>
    public int AccessFailedCount { get; set; } = 0;
    
    /// <summary>
    /// Hesab bloklanma tarixi
    /// </summary>
    public DateTime? LockoutEnd { get; set; }
    
    #endregion
    
    #region Active Directory
    
    /// <summary>
    /// Autentifikasiya tipi
    /// </summary>
    public AuthenticationType AuthenticationType { get; set; } = AuthenticationType.Local;
    
    /// <summary>
    /// Active Directory Domain
    /// </summary>
    public string? Domain { get; set; }
    
    /// <summary>
    /// Active Directory SID
    /// </summary>
    public string? ActiveDirectorySid { get; set; }
    
    /// <summary>
    /// AD qrup üzvlükləri (vergüllə ayrılmış)
    /// </summary>
    public string? AdGroups { get; set; }
    
    /// <summary>
    /// AD istifadəçisi ilk dəfə login edibmi (profil tamamlanıbmı?)
    /// </summary>
    public bool IsProfileCompleted { get; set; } = false;
    
    #endregion
    
    #region Common
    
    /// <summary>
    /// Sistem rolu (Admin, Manager, User)
    /// </summary>
    public UserRole Role { get; set; } = UserRole.User;
    
    /// <summary>
    /// Hesab aktivdir?
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Avatar/Profil şəkli URL-i
    /// </summary>
    public string? AvatarUrl { get; set; }
    
    /// <summary>
    /// Son giriş tarixi
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// Yaradılma tarixi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Son dəyişiklik tarixi
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
    
    #endregion
    
    // Navigation properties
    public ICollection<UserProjectRole> ProjectRoles { get; set; } = new List<UserProjectRole>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

public enum AuthenticationType
{
    /// <summary>
    /// Local qeydiyyat (Email + şifrə)
    /// </summary>
    Local,
    
    /// <summary>
    /// Active Directory autentifikasiyası
    /// </summary>
    ActiveDirectory
}

public enum UserRole
{
    User,       // Adi istifadəçi
    Manager,    // Menecer
    Admin,      // Sistem administratoru
    SuperAdmin  // Super administrator
}

public class UserProjectRole
{
    public long UserId { get; set; }
    public long ProjectId { get; set; }
    public ProjectRole Role { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    public User User { get; set; } = null!;
    public Project Project { get; set; } = null!;
}

public enum ProjectRole
{
    Owner,    // Tam icazə
    Admin,    // İdarəetmə icazələri
    Member,   // Əsas iş icazələri
    Viewer    // Yalnız baxış
}
