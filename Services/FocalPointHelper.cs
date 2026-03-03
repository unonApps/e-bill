using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using TAB.Web.Models;

namespace TAB.Web.Services;

public static class FocalPointHelper
{
    public static bool IsFocalPoint(ClaimsPrincipal principal)
        => principal.IsInRole("Agency Focal Point") && !principal.IsInRole("Admin");

    /// <summary>
    /// Returns the OrganizationId to scope queries to.
    /// Returns null for Admin (sees all). Returns a non-null value for Focal Point.
    /// Returns -1 if Focal Point has no OrganizationId configured (sees nothing — safe default).
    /// </summary>
    public static async Task<int?> GetScopedOrgIdAsync(
        ClaimsPrincipal principal, UserManager<ApplicationUser> userManager)
    {
        if (!IsFocalPoint(principal)) return null;
        var user = await userManager.GetUserAsync(principal);
        return user?.OrganizationId ?? -1;
    }
}
