using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WeatherVibez.Controllers;
using WeatherVibez.Models;
using WeatherVibez.ViewModels;

public class UsersController : Controller
{
	private readonly UserManager<User> _userManager;
	private readonly SignInManager<User> _signInManager;

	// Constructor to initialize UserManager and SignInManager
	public UsersController(UserManager<User> userManager, SignInManager<User> signInManager, RoleManager<IdentityRole> roleManager, ILogger<HomeController> logger)
	{
		_userManager = userManager;
		_signInManager = signInManager;
	}

	// GET: /Users/Overview
	// Retrieves a list of all users with their roles and balance
	public async Task<IActionResult> Overview()
	{
		var users = _userManager.Users.ToList(); // Get all users
		var usersWithRoles = new List<UserDetails>(); // Create a list to hold users with their roles

		foreach (var user in users)
		{
			var roles = await _userManager.GetRolesAsync(user); // Get the roles for each user
			usersWithRoles.Add(new UserDetails
			{
				User = user,
				Roles = roles,
				Balance = user.Balance
			});
		}

		return View(usersWithRoles); // Pass the users with roles to the view
	}

	// GET: /Users/TestingAdmin
	// Checks for the presence of a JWT token and returns the view if authenticated
	[Authorize]
	public IActionResult TestingAdmin()
	{
		var token = Request.Cookies["jwtToken"];
		if (string.IsNullOrEmpty(token))
		{
			return Unauthorized(); // Token not found, return 401 Unauthorized
		}

		// You can now decode and validate the token here or just proceed with your page logic.
		return View();
	}

	// GET: /Users/Create
	// Displays the create user view
	public IActionResult Create()
	{
		ViewData["Title"] = "Create User";
		return View(new User());
	}

	// POST: /Users/Create
	// Handles the creation of a new user
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create([Bind("UserName,Email")] User user, string? password)
	{
		// Validate the password
		if (string.IsNullOrWhiteSpace(password))
		{
			ModelState.AddModelError("Password", "Password is required.");
			return View(user);
		}

		if (ModelState.IsValid)
		{
			var newUser = new User
			{
				UserName = user.UserName,
				Email = user.Email
			};

			var result = await _userManager.CreateAsync(newUser, password);

			if (result.Succeeded)
			{
				// Adding only the first created user as an Admin
				// isPersistent: false means the cookie will be deleted when the browser is closed
				await _signInManager.SignInAsync(newUser, isPersistent: false);

				if (await _userManager.Users.CountAsync() == 1)
				{
					await _userManager.AddToRoleAsync(newUser, "Admin");
				}
				else
				{
					await _userManager.AddToRoleAsync(newUser, "User");
				}
				// Automatically sign in the new user
				return RedirectToAction("SearchWeather", "Weather");
			}
			else
			{
				foreach (var error in result.Errors)
				{
					ModelState.AddModelError(string.Empty, error.Description);
				}
			}
		}
		return View(user);
	}
}

