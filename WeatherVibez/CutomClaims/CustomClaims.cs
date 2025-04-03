using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using WeatherVibez.Models;

namespace WeatherVibez.CustomClaims
{
	// CustomClaims class to add custom claims to the user's identity
	public class CustomClaims : UserClaimsPrincipalFactory<User, IdentityRole>
	{
		// Constructor to initialize UserManager, RoleManager, and IdentityOptions
		public CustomClaims(
			UserManager<User> userManager,
			RoleManager<IdentityRole> roleManager,
			IOptions<IdentityOptions> optionsAccessor)
			: base(userManager, roleManager, optionsAccessor){}

		// Generates custom claims for the user
		protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
		{
			var identity = await base.GenerateClaimsAsync(user);
			var roles = await UserManager.GetRolesAsync(user);
			foreach (var role in roles)
			{
				identity.AddClaim(new Claim(ClaimTypes.Role, role));
			}
			return identity;
		}
	}
}

