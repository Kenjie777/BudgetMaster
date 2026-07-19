using BudgetMasterFinal.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace BudgetMasterFinal.Services
{
    public class RoleClaimsTransformation : IClaimsTransformation
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleClaimsTransformation(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(principal);
                if (user != null && !string.IsNullOrEmpty(user.Role))
                {
                    var identity = (ClaimsIdentity)principal.Identity;
                    
                    // Remove existing role claims
                    var existingRoleClaims = identity.FindAll("Role").ToList();
                    foreach (var claim in existingRoleClaims)
                    {
                        identity.RemoveClaim(claim);
                    }
                    
                    var existingStandardRoleClaims = identity.FindAll(ClaimTypes.Role).ToList();
                    foreach (var claim in existingStandardRoleClaims)
                    {
                        identity.RemoveClaim(claim);
                    }
                    
                    // Add both custom Role claim and standard role claim
                    identity.AddClaim(new Claim("Role", user.Role));
                    identity.AddClaim(new Claim(ClaimTypes.Role, user.Role));
                }
            }
            
            return principal;
        }
    }
}