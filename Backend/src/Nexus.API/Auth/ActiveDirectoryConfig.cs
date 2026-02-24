using System.DirectoryServices.AccountManagement;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;

namespace Nexus.API.Auth;

/// <summary>
/// Active Directory Authentication Configuration
/// </summary>
public static class ActiveDirectoryConfig
{
    public static IServiceCollection AddActiveDirectoryAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var adSettings = configuration.GetSection("ActiveDirectory").Get<ActiveDirectorySettings>();
        
        if (adSettings?.Enabled != true)
        {
            services.AddSingleton(adSettings ?? new ActiveDirectorySettings { Enabled = false });
            return services;
        }

        services.AddSingleton(adSettings);

        // Windows Authentication (Negotiate = Kerberos + NTLM)
        services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
            .AddNegotiate(options =>
            {
                // Kerberos/NTLM settings
                if (!string.IsNullOrEmpty(adSettings.Domain))
                {
                    //options.Domain = adSettings.Domain;
                }
            });

        services.AddAuthorization(options =>
        {
            // Default policy requires authentication
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Admin policy - requires Admin group membership
            options.AddPolicy("RequireAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                {
                    var user = context.User;
                    if (!user.Identity?.IsAuthenticated ?? false)
                        return false;

                    // Check AD group membership
                    var adminGroups = adSettings.AdminGroups?.Split(',') ?? new[] { "Domain Admins" };
                    return adminGroups.Any(group => 
                        user.IsInRole($"{adSettings.Domain}\\{group.Trim()}"));
                });
            });

            // User policy - requires User group membership
            options.AddPolicy("RequireUser", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                {
                    var user = context.User;
                    if (!user.Identity?.IsAuthenticated ?? false)
                        return false;

                    var userGroups = adSettings.UserGroups?.Split(',') ?? new[] { "Domain Users" };
                    return userGroups.Any(group => 
                        user.IsInRole($"{adSettings.Domain}\\{group.Trim()}"));
                });
            });
        });

        return services;
    }

    public static IApplicationBuilder UseActiveDirectoryAuthentication(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}

public class ActiveDirectorySettings
{
    public bool Enabled { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string LdapPath { get; set; } = string.Empty;
    public string UserGroups { get; set; } = "NexusPM_Users";
    public string AdminGroups { get; set; } = "NexusPM_Admins";
    public bool AutoCreateUsers { get; set; } = true;
    public string DefaultOrganization { get; set; } = "AZNEFT_IB";
}

/// <summary>
/// Active Directory User Service
/// </summary>
public interface IActiveDirectoryService
{
    Task<AdUserInfo?> GetUserInfoAsync(string username);
    Task<bool> ValidateCredentialsAsync(string username, string password);
    Task<IEnumerable<AdUserInfo>> GetUsersInGroupAsync(string groupName);
    Task<bool> IsUserInGroupAsync(string username, string groupName);
    Task<IEnumerable<AdGroupInfo>> GetUserGroupsAsync(string username);
}

public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly ActiveDirectorySettings _settings;
    private readonly ILogger<ActiveDirectoryService> _logger;
    private readonly PrincipalContext _context;

    public ActiveDirectoryService(
        ActiveDirectorySettings settings,
        ILogger<ActiveDirectoryService> logger)
    {
        _settings = settings;
        _logger = logger;

        if (!settings.Enabled)
        {
            _context = null!;
            return;
        }

        try
        {
            _context = string.IsNullOrEmpty(settings.Domain)
                ? new PrincipalContext(ContextType.Domain)
                : new PrincipalContext(ContextType.Domain, settings.Domain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Active Directory context");
            throw;
        }
    }

    public async Task<AdUserInfo?> GetUserInfoAsync(string username)
    {
        if (!_settings.Enabled || _context == null)
            return null;

        return await Task.Run(() =>
        {
            try
            {
                var userPrincipal = UserPrincipal.FindByIdentity(_context, username);
                if (userPrincipal == null)
                    return null;

                return new AdUserInfo
                {
                    UserName = userPrincipal.SamAccountName,
                    DisplayName = userPrincipal.DisplayName,
                    Email = userPrincipal.EmailAddress,
                    GivenName = userPrincipal.GivenName,
                    Surname = userPrincipal.Surname,
                    DistinguishedName = userPrincipal.DistinguishedName,
                    Enabled = userPrincipal.Enabled ?? false,
                    LastLogon = userPrincipal.LastLogon,
                    Groups = GetUserGroupNames(userPrincipal)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get AD user info for {Username}", username);
                return null;
            }
        });
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        if (!_settings.Enabled || _context == null)
            return false;

        return await Task.Run(() =>
        {
            try
            {
                return _context.ValidateCredentials(username, password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate credentials for {Username}", username);
                return false;
            }
        });
    }

    public async Task<IEnumerable<AdUserInfo>> GetUsersInGroupAsync(string groupName)
    {
        if (!_settings.Enabled || _context == null)
            return Enumerable.Empty<AdUserInfo>();

        return await Task.Run(() =>
        {
            try
            {
                var group = GroupPrincipal.FindByIdentity(_context, groupName);
                if (group == null)
                    return Enumerable.Empty<AdUserInfo>();

                return group.GetMembers(true)
                    .OfType<UserPrincipal>()
                    .Select(u => new AdUserInfo
                    {
                        UserName = u.SamAccountName,
                        DisplayName = u.DisplayName,
                        Email = u.EmailAddress,
                        Enabled = u.Enabled ?? false
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get users in group {GroupName}", groupName);
                return Enumerable.Empty<AdUserInfo>();
            }
        });
    }

    public async Task<bool> IsUserInGroupAsync(string username, string groupName)
    {
        if (!_settings.Enabled || _context == null)
            return false;

        return await Task.Run(() =>
        {
            try
            {
                var user = UserPrincipal.FindByIdentity(_context, username);
                if (user == null)
                    return false;

                var groups = GetUserGroupNames(user);
                return groups.Contains(groupName, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check group membership for {Username}", username);
                return false;
            }
        });
    }

    public async Task<IEnumerable<AdGroupInfo>> GetUserGroupsAsync(string username)
    {
        if (!_settings.Enabled || _context == null)
            return Enumerable.Empty<AdGroupInfo>();

        return await Task.Run(() =>
        {
            try
            {
                var user = UserPrincipal.FindByIdentity(_context, username);
                if (user == null)
                    return Enumerable.Empty<AdGroupInfo>();

                return user.GetGroups()
                    .OfType<GroupPrincipal>()
                    .Select(g => new AdGroupInfo
                    {
                        Name = g.Name,
                        SamAccountName = g.SamAccountName,
                        Description = g.Description
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get groups for {Username}", username);
                return Enumerable.Empty<AdGroupInfo>();
            }
        });
    }

    private List<string> GetUserGroupNames(UserPrincipal user)
    {
        try
        {
            return user.GetGroups()
                .OfType<GroupPrincipal>()
                .Select(g => g.Name)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }
}

public class AdUserInfo
{
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? DistinguishedName { get; set; }
    public bool Enabled { get; set; }
    public DateTime? LastLogon { get; set; }
    public List<string> Groups { get; set; } = new();
}

public class AdGroupInfo
{
    public string Name { get; set; } = string.Empty;
    public string SamAccountName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
