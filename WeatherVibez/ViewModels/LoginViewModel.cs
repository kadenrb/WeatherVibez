using System.ComponentModel.DataAnnotations;

namespace WeatherVibez.ViewModels
{
	// ViewModel for user login
	public class LoginViewModel
	{
		// User's email address
		[Required]
		public string? Email { get; set; }

		// User's password
		[Required]
		public string? Password { get; set; }
	}
}

