using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SmartShift.Blazor.Services;

/// <summary>
/// Transforms user claims to add supervisor and admin roles based on Azure AD group membership.
/// Requires Azure AD to be configured to emit group claims in the token.
/// </summary>
public class SmartShiftClaims : IClaimsTransformation
{
    private readonly ILogger<SmartShiftClaims> _logger;
    private readonly RoleOptions _options;

    public SmartShiftClaims(
        ILogger<SmartShiftClaims> logger,
        IOptions<RoleOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null)
        {
            _logger.LogWarning("No ClaimsIdentity found in principal");
            return Task.FromResult(principal);
        }
         
        // Get user identifiers
        var userEmail = principal.FindFirst(ClaimTypes.Email)?.Value;
        var userName = principal.FindFirst(ClaimTypes.Name)?.Value;
        var groups = principal.FindAll(ClaimTypes.GroupSid).Select(c => c.Value).ToList();
        var userUpn = principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn")?.Value;
        var userObjectId = principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

        // Azure AD fallbacks - these claims are often in different formats
        var preferredUsername = principal.FindFirst("preferred_username")?.Value;
        var shortName = principal.FindFirst("name")?.Value;

        _logger.LogDebug(
            "Raw claims found - Email: {Email}, Name: {Name}, UPN: {Upn}, ObjectId: {ObjectId}, PreferredUsername: {PreferredUsername}, ShortName: {ShortName}",
            userEmail ?? "null",
            userName ?? "null",
            userUpn ?? "null",
            userObjectId ?? "null",
            preferredUsername ?? "null",
            shortName ?? "null");

        // Try to identify the user - prefer email, then UPN, then username, then name
        var identifier = userEmail ?? preferredUsername ?? userUpn ?? userName ?? shortName;

        if (string.IsNullOrEmpty(identifier))
        {
            // Log all available claims for debugging
            var allClaims = identity.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
            var claimsStr = string.Join("; ", allClaims);
        
            _logger.LogWarning(
                "Could not determine user identifier for claims transformation. Available claims: {Claims}",
                claimsStr);
            return Task.FromResult(principal);
        }

        _logger.LogDebug("Processing claims for user: {UserIdentifier} (ObjectId: {ObjectId})", identifier, userObjectId ?? "N/A");

        // Check and assign Supervisor role
        if (IsSupervisor(principal, identifier))
        {
            if (!identity.HasClaim(ClaimTypes.Role, "Supervisor"))
            {
                _logger.LogInformation("Adding Supervisor role to user: {UserIdentifier}", identifier);
                identity.AddClaim(new Claim(ClaimTypes.Role, "Supervisor"));
            }
        }

        // Check and assign Admin role
        if (IsAdmin(principal, identifier))
        {
            if (!identity.HasClaim(ClaimTypes.Role, "Admin"))
            {
                _logger.LogInformation("Adding Admin role to user: {UserIdentifier}", identifier);
                identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
            }
        }

        return Task.FromResult(principal);
    }

    private bool IsSupervisor(ClaimsPrincipal principal, string userIdentifier)
    {
        // Check if user email is in supervisor emails list
        if (IsUserInEmailList(userIdentifier, _options.SupervisorEmails))
        {
            _logger.LogDebug("User {UserIdentifier} matched supervisor email list", userIdentifier);
            return true;
        }

        // Check if user is in supervisor Azure AD groups
        if (IsUserInGroups(principal, _options.SupervisorGroupIds))
        {
            _logger.LogDebug("User {UserIdentifier} matched supervisor Azure AD group", userIdentifier);
            return true;
        }

        return false;
    }

    private bool IsAdmin(ClaimsPrincipal principal, string userIdentifier)
    {
        // Check if user email is in admin emails list
        if (IsUserInEmailList(userIdentifier, _options.AdminEmails))
        {
            _logger.LogDebug("User {UserIdentifier} matched admin email list", userIdentifier);
            return true;
        }

        // Check if user is in admin Azure AD groups
        if (IsUserInGroups(principal, _options.AdminGroupIds))
        {
            _logger.LogDebug("User {UserIdentifier} matched admin Azure AD group", userIdentifier);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the user identifier (email/UPN) is in the specified email list.
    /// </summary>
    private bool IsUserInEmailList(string userIdentifier, List<string>? emails)
    {
        if (emails == null || emails.Count == 0)
        {
            return false;
        }

        return emails.Any(e => string.Equals(e, userIdentifier, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the user belongs to any of the specified Azure AD groups by group ID.
    /// Reads group membership directly from token claims (requires Azure AD token configuration).
    /// </summary>
    private bool IsUserInGroups(ClaimsPrincipal principal, List<string>? groupIds)
    {
        if (groupIds == null || groupIds.Count == 0)
        {
            return false;
        }

        // Log all claims for debugging
        var allClaims = principal.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
        _logger.LogDebug("All claims in token: {Claims}", string.Join("; ", allClaims));

        // Azure AD can emit groups under different claim types depending on configuration
        var possibleGroupClaimTypes = new[]
        {
            "groups",
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/groups",
            "http://schemas.microsoft.com/claims/groups",
            ClaimTypes.GroupSid,  // http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid
            "wids"  // Directory role IDs
        };

        var userGroupIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claimType in possibleGroupClaimTypes)
        {
            var groupClaims = principal.FindAll(claimType).Select(c => c.Value);
            foreach (var groupClaim in groupClaims)
            {
                userGroupIds.Add(groupClaim);
                _logger.LogDebug("Found group claim [{ClaimType}]: {GroupId}", claimType, groupClaim);
            }
        }

        if (userGroupIds.Count == 0)
        {
            _logger.LogWarning("No group claims found in token. Ensure 'groups' claim is configured in Azure AD App Registration > Token configuration, and the user has signed out and back in.");
            return false;
        }

        _logger.LogDebug("User has {GroupCount} total group claims", userGroupIds.Count);

        // Check if user is in any of the configured groups
        foreach (var groupId in groupIds)
        {
            if (userGroupIds.Contains(groupId))
            {
                _logger.LogDebug("User matched configured group: {GroupId}", groupId);
                return true;
            }
        }

        _logger.LogDebug("User not in any configured groups. User groups: [{UserGroups}], Configured groups: [{ConfiguredGroups}]",
            string.Join(", ", userGroupIds),
            string.Join(", ", groupIds));

        return false;
    }
}

/// <summary>
/// Configuration options for role assignment based on Azure AD group IDs
/// </summary>
public class RoleOptions
{
    public const string SectionName = "Authorization:Roles";

    /// <summary>
    /// List of Azure AD group Object IDs that should receive Supervisor role.
    /// Find group IDs in Azure Portal > Microsoft Entra ID > Groups > [Group] > Object ID
    /// </summary>
    public List<string> SupervisorGroupIds { get; set; } = new();


    /// <summary>
    /// List of Azure AD group Object IDs that should receive Admin role.
    /// Find group IDs in Azure Portal > Microsoft Entra ID > Groups > [Group] > Object ID
    /// </summary>
    public List<string> AdminGroupIds { get; set; } = new();

    /// <summary>
    /// List of email addresses that should receive Supervisor role.
    /// </summary>
    public List<string> SupervisorEmails { get; set; } = new();

    /// <summary>
    /// List of email addresses that should receive Admin role.
    /// </summary>
    public List<string> AdminEmails { get; set; } = new();
}


