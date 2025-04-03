using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WeatherVibez.Models
{
	//!! Using Identity to manage users and roles (DATA IS STORED IN IDENTITY IN THE DATABASE!!! LIKE USER
	// AND PASSWORD)
	public class User : IdentityUser
	{
		// Balnce for user balance (used for premium users)
		[RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Only up to two decimal places are allowed.")]
		public decimal Balance { get; set; } = 0;
		// Used to track payments for a paper trail (was reccomended by Stripe)
		public string? StripeCustomerId { get; set; }
		// Used to store the premium users chosen city
		public string? City { get; set; }

	}
}
