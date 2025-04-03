using Microsoft.AspNetCore.Identity;
using WeatherVibez.Models;

namespace WeatherVibez.ViewModels
{
	// ViewModel for user details including roles and balance
	public class UserDetails
	{
		// The user object
		public User? User { get; set; }
		// List of roles assigned to the user
		public IList<string>? Roles { get; set; }
		// User's balance, with getter and setter
		public decimal Balance
		{
			get => User?.Balance ?? 0;
			set => User!.Balance = value;
		}
	}
}

