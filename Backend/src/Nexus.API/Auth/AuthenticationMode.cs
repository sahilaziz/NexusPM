namespace Nexus.API.Auth;

/// <summary>
/// Sistemdə istifadə olunacaq autentifikasiya rejimi
/// </summary>
public enum AuthenticationMode
{
    /// <summary>
    /// Local qeydiyyat: Email, şifrə, 2FA dəstəyi
    /// </summary>
    Local,
    
    /// <summary>
    /// Active Directory: Windows Domain autentifikasiyası
    /// </summary>
    ActiveDirectory,
    
    /// <summary>
    /// Hər iki rejim aktiv (istifadəçi seçə bilər)
    /// </summary>
    Mixed
}

/// <summary>
/// Autentifikasiya konfiqurasiyası
/// </summary>
public class AuthenticationConfig
{
    public AuthenticationMode Mode { get; set; } = AuthenticationMode.Local;
    
    /// <summary>
    /// Local rejim parametrləri
    /// </summary>
    public LocalAuthConfig Local { get; set; } = new();
    
    /// <summary>
    /// Active Directory parametrləri
    /// </summary>
    public ActiveDirectoryAuthConfig ActiveDirectory { get; set; } = new();
}

public class LocalAuthConfig
{
    /// <summary>
    /// Email təsdiqi tələb olunurmu?
    /// </summary>
    public bool RequireEmailConfirmation { get; set; } = true;
    
    /// <summary>
    /// 2FA (TOTP) dəstəyi aktivdirmi?
    /// </summary>
    public bool EnableTwoFactor { get; set; } = true;
    
    /// <summary>
    /// İstifadəçi özü 2FA aktiv edə bilərmi?
    /// </summary>
    public bool AllowUserTwoFactorSetup { get; set; } = true;
    
    /// <summary>
    /// Şifrə siyasəti
    /// </summary>
    public PasswordPolicyConfig PasswordPolicy { get; set; } = new();
    
    /// <summary>
    /// Uğursuz giriş cəhdlərinin sayı (bloklanmadan əvvəl)
    /// </summary>
    public int MaxFailedAccessAttempts { get; set; } = 5;
    
    /// <summary>
    /// Bloklama müddəti (dəqiqə)
    /// </summary>
    public int LockoutDurationMinutes { get; set; } = 30;
}

public class PasswordPolicyConfig
{
    public int RequiredLength { get; set; } = 8;
    public bool RequireDigit { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; } = true;
}

public class ActiveDirectoryAuthConfig
{
    /// <summary>
    /// Domain adı (məsələn: COMPANY.COM)
    /// </summary>
    public string Domain { get; set; } = string.Empty;
    
    /// <summary>
    /// LDAP server ünvanı
    /// </summary>
    public string LdapServer { get; set; } = string.Empty;
    
    /// <summary>
    /// LDAP port (default: 636 for SSL)
    /// </summary>
    public int LdapPort { get; set; } = 636;
    
    /// <summary>
    /// SSL istifadə edilsinmi?
    /// </summary>
    public bool UseSsl { get; set; } = true;
    
    /// <summary>
    /// İstifadəçi axtarışı üçün OU (Organizational Unit)
    /// </summary>
    public string? SearchBase { get; set; }
    
    /// <summary>
    /// Admin qrup adları (sistemdə admin səlahiyyəti veriləcək)
    /// </summary>
    public List<string> AdminGroups { get; set; } = new() { "Domain Admins", "NexusPM_Admins" };
    
    /// <summary>
    /// İstifadəçi qrup adları (sistemə giriş icazəsi)
    /// </summary>
    public List<string> UserGroups { get; set; } = new() { "NexusPM_Users" };
}
