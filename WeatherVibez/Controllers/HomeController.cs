using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WeatherVibez.Models;
using WeatherVibez.ViewModels;
using WeatherVibez.Services; // Add the namespace for JwtService
using Microsoft.Extensions.Configuration;

namespace WeatherVibez.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly SignInManager<User> _signInManager;
		private readonly UserManager<User> _userManager;
		private readonly JwtService _jwtService;

		// Constructor to initialize dependencies
		public HomeController(ILogger<HomeController> logger, UserManager<User> userManager, SignInManager<User> signInManager, JwtService jwtService)
		{
			_logger = logger;
			_userManager = userManager;
			_signInManager = signInManager;
			_jwtService = jwtService;
		}

		// GET: /Home/Index
		// Displays the home page
		public IActionResult Index()
		{
			return View();
		}

		// GET: /Home/Privacy
		// Displays the privacy policy page
		public IActionResult Privacy()
		{
			return View();
		}

		// GET: /login
		// Displays the login page
		[Route("login")]
		public IActionResult Login()
		{
			return View();
		}

		// POST: /login
		// Handles the login process
		[Route("login")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(LoginViewModel model)
		{
			if (ModelState.IsValid)
			{
				// Find the user by email
				var user = await _userManager.FindByEmailAsync(model.Email);
				if (user != null)
				{
					// Attempt to sign in the user
					var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
					if (result.Succeeded)
					{
						// Generate JWT token for the user
						var token = await _jwtService.GenerateJwtToken(user.Id, user.UserName);
						_logger.LogInformation("JWT token generated");

						// Set the token in a cookie with appropriate options
						Response.Cookies.Append("jwtToken", token, new CookieOptions
						{
							HttpOnly = true,  // Makes the cookie inaccessible from JavaScript
							Secure = false, // Only set the Secure flag if HTTPS is used
							SameSite = SameSiteMode.Strict, // Prevent cross-site request forgery
							Expires = DateTime.UtcNow.AddMinutes(20) // Token expiration
						});

						// Redirect to SearchWeather action in WeatherController
						return RedirectToAction("SearchWeather", "Weather");
					}
					else
					{
						ModelState.AddModelError(string.Empty, "Invalid login attempt.");
					}
				}
				else
				{
					ModelState.AddModelError(string.Empty, "User not found.");
				}
			}
			else
			{
				ModelState.AddModelError(string.Empty, "Model is invalid");
			}

			return BadRequest(ModelState); // Return BadRequest instead of redirecting
		}
	}
}
