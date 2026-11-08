using System.Security.Claims;

namespace AuthDemo.Extensions
{
    // We don't control how Module 01's login builds the token, so these helpers accept the
    // common claim names instead of assuming one.
    public static class ClaimsExtensions
    {
        private static readonly string[] UserIdClaimTypes =
        {
            ClaimTypes.NameIdentifier, "nameid", "sub", "id", "userId", "uid"
        };

        // The logged-in user's id, as text (works for int, Guid or string ids).
        public static string? GetUserId(this ClaimsPrincipal user)
        {
            foreach (var type in UserIdClaimTypes)
            {
                var value = user.FindFirst(type)?.Value;
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
            return null;
        }

        // Every role value found in the token (handles "role", "roles" and the long ClaimTypes.Role).
        public static List<string> GetRoles(this ClaimsPrincipal user)
        {
            return user.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles")
                .Select(c => c.Value)
                .ToList();
        }
    }
}
