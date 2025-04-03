using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using VibeVaultC_.Models;
using WeatherVibez.Models;

namespace WeatherVibez.Controllers
{
	public class PremiumController : Controller
	{
		private readonly IConfiguration _configuration;
		private readonly UserManager<User> _userManager;
		private readonly IServiceProvider _serviceProvider;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly SignInManager<User> _signInManager;

		// Constructor to initialize dependencies
		public PremiumController(
			UserManager<User> userManager,
			IConfiguration configuration,
			IServiceProvider serviceProvider,
			IHttpClientFactory httpClientFactory,
			SignInManager<User> signInManager)
		{
			_configuration = configuration;
			_userManager = userManager;
			_serviceProvider = serviceProvider;
			_httpClientFactory = httpClientFactory;
			_signInManager = signInManager;
		}

		// GET: /Premium/PurchasePremium
		// Displays the purchase premium view
		public IActionResult PurchasePremium()
		{
			return View();
		}

		// POST: /Premium/PurchasePremiumConfiremed
		// Confirms the purchase of premium membership
		[HttpPost]
		public async Task<IActionResult> PurchasePremiumConfiremed()
		{
			// Get the current user
			var user = await _userManager.GetUserAsync(User);
			const int premiumPrice = 6; // The amount to deduct

			// Check if the user has sufficient balance
			if (user.Balance < premiumPrice)
			{
				TempData["Error"] = "Insufficient balance. Please add funds to your account.";
				return RedirectToAction("Balance", "Payments");
			}

			// Subtract premium price from the balance
			user.Balance -= premiumPrice;
			var result = await _userManager.UpdateAsync(user);
			if (!result.Succeeded)
			{
				TempData["Error"] = "Failed to validate balance, contact admin if error persists.";
				return RedirectToAction("Balance", "Payments");
			}

			// Add the Premium role to the user
			await _userManager.AddToRoleAsync(user, "Premium");

			// Force user claims refresh by signing in again
			await _signInManager.RefreshSignInAsync(user);

			return RedirectToAction("PremiumNotifications", "Premium");
		}

		// GET: /Premium/PremiumNotifications
		// Displays the premium notifications view
		[HttpGet]
		public IActionResult PremiumNotifications()
		{
			return View();
		}

		// POST: /Premium/PremiumNotifications
		// Handles the premium notifications settings for the user
		[HttpPost]
		[Authorize(Roles = "Premium,Admin")]
		public async Task<IActionResult> PremiumNotifications(string phoneNumber, string city)
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return Challenge();

			// Toggle notifications: if phoneNumber is "0", disable notifications; otherwise enable.
			user.PhoneNumber = phoneNumber == "0" ? null : $"+1{phoneNumber}";
			user.City = city;

			await _userManager.UpdateAsync(user);

			// Start the daily notification background task to figure out the next 6 AM and 
			// prepare the notification if it is by saving the task to the user
			_ = Task.Run(() => SchedulePremiumNotification(user.Id));
			SendPremiumNotification(user).Wait();
			return RedirectToAction("CompareWeather", "Weather", new { city });
		}

		// Schedules the premium notification for the user
		private async Task SchedulePremiumNotification(string userId)
		{
			try
			{
				// Create a new scope for background processing.
				using IServiceScope scope = _serviceProvider.CreateScope();
				var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

				// Calculate initial delay until next 6 AM.
				DateTime now = DateTime.Now;
				DateTime next6AM = now.Date.AddHours(6); // today's 6 AM
				if (now >= next6AM)
				{
					next6AM = next6AM.AddDays(1); // if passed, schedule for tomorrow
				}
				TimeSpan initialDelay = next6AM - now;
				await Task.Delay(initialDelay);

				// Start a PeriodicTimer that ticks every 24 hours.
				using PeriodicTimer timer = new(TimeSpan.FromDays(1));
				while (await timer.WaitForNextTickAsync())
				{
					// Re-fetch the user to check that they still have notifications enabled.
					var user = await userManager.FindByIdAsync(userId);
					if (user == null || user.PhoneNumber == null)
					{
						// Exit if the user has been removed or disabled notifications.
						break;
					}

					// Execute the notification logic.
					await SendPremiumNotification(user);
				}
			}
			catch (Exception ex)
			{
				// Log the error, if you have a logging mechanism.
				Console.WriteLine($"Error in SchedulePremiumNotification: {ex.Message}");
			}
		}

		// Sends the premium notification to the user
		private async Task SendPremiumNotification(User user)
		{
			try
			{
				// Retrieve Twilio configuration values.
				var accountSid = _configuration["Twilio:AccountSid"];
				var authToken = _configuration["Twilio:AuthToken"];
				var fromNumber = _configuration["Twilio:PhoneNumber"];

				TwilioClient.Init(accountSid, authToken);

				// Retrieve weather details using the API.
				var weather = await GetWeatherAsync(user.City);
				if (weather == null)
				{
					Console.WriteLine("Weather data unavailable.");
					return;
				}

				// Create and send the SMS message.
				var messageOptions = new CreateMessageOptions(new PhoneNumber(user.PhoneNumber))
				{
					From = new PhoneNumber(fromNumber),
					Body = $"Weather Alert for {user.City}: \n" +
						   $"High: {weather.TempMax}°C \n" +
						   $"Low: {weather.TempMin}°C \n" +
						   $"Humidity: {weather.Humidity}%\n" +
						   $"The weather is {weather.Description}"
				};

				var message = await MessageResource.CreateAsync(messageOptions);
				Console.WriteLine($"Sent SMS SID: {message.Sid}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"SMS failed: {ex.Message}");
			}
		}

		// Retrieves weather information for the specified city
		private async Task<WeatherModel?> GetWeatherAsync(string city)
		{
			try
			{
				// Create an HTTP client to call the OpenWeather API and fetch weather data.
				var client = _httpClientFactory.CreateClient();
				var apiKey = _configuration["OpenWeather:ApiKey"];
				var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&units=metric";

				var response = await client.GetFromJsonAsync<OpenWeatherResponse>(url);
				if (response == null)
				{
					return null;
				}

				return new WeatherModel
				{
					// Update the response to the WeatherModel
					City = city,
					Temperature = response.Main.Temp,
					TempMin = response.Main.Temp_min,
					TempMax = response.Main.Temp_max,
					Humidity = response.Main.Humidity,
					Description = response.Weather.FirstOrDefault()?.Description ?? "No description" // if null return "No description"
				};
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error fetching weather: {ex.Message}");
				return null;
			}
		}
	}
}
