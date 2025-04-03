using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WeatherVibez.Models
{
	public class UserDbContext : IdentityDbContext<User>
	{
		// Constructor to initialize the DbContext with options
		public UserDbContext(DbContextOptions<UserDbContext> options)
			: base(options){}
	}
}
