using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using WeatherVibez.Models;

namespace WeatherVibez.Services
{
	// Service to generate JWT tokens for authenticated users
	public class JwtService
	{
		private readonly IConfiguration _configuration;
		private readonly UserManager<User> _userManager;

		// Constructor to initialize IConfiguration and UserManager
		public JwtService(IConfiguration configuration, UserManager<User> userManager)
		{
			_configuration = configuration;
			_userManager = userManager;
		}

		// Generates a JWT token for the specified user
		public async Task<string> GenerateJwtToken(string userId, string userName)
		{
			var secretKey = _configuration["Jwt:Secret"];
			var issuer = _configuration["Jwt:Issuer"];
			var audience = _configuration["Jwt:Audience"];

			// Find the user by ID
			var user = await _userManager.FindByIdAsync(userId);
			if (user == null)
			{
				throw new Exception("User not found");
			}

			// Get the roles for the user
			var roles = await _userManager.GetRolesAsync(user);

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
			var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			// Create claims for the token
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, userId),
				new Claim(ClaimTypes.Name, userName)
			};

			// Add role claims to the token
			foreach (var role in roles)
			{
				claims.Add(new Claim(ClaimTypes.Role, role));
			}

			// Define the token descriptor
			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(claims),
				Expires = DateTime.UtcNow.AddMinutes(20),
				Issuer = issuer,
				Audience = audience,
				SigningCredentials = credentials
			};

			var tokenHandler = new JwtSecurityTokenHandler();
			var token = tokenHandler.CreateToken(tokenDescriptor);

			// Return the generated token
			return tokenHandler.WriteToken(token);
		}
	}
}

