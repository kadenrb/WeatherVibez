using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using VibeVaultC_.Models;
using WeatherVibez.ViewModels;
using WeatherVibez.Models;

namespace WeatherVibez.Controllers
{
	[Authorize]
	public class WeatherController : Controller
	{
		private readonly IConfiguration _configuration;
		private readonly IHttpClientFactory _httpClientFactory;

		// Constructor to initialize IConfiguration and IHttpClientFactory
		public WeatherController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
		{
			_configuration = configuration;
			_httpClientFactory = httpClientFactory;
		}

		// GET: /Weather/SearchWeather
		// Displays the search weather view
		[HttpGet]
		public IActionResult SearchWeather()
		{
			return View();
		}

		// POST: /Weather/SearchWeather
		// Handles the search weather request and sends an SMS alert if a phone number is provided
		[HttpPost]
		public async Task<IActionResult> SearchWeather(string city, string phoneNumber)
		{
			try
			{
				var weather = await GetWeatherAsync(city);

				if (!string.IsNullOrEmpty(phoneNumber))
				{
					await SendWeatherAlert(phoneNumber, weather);
					TempData["Alert"] = "SMS sent!";
				}

				return View(weather); // Show same page with data
			}
			catch (Exception ex)
			{
				TempData["Error"] = "Error: " + ex.Message;
				return View(); // Re-show empty form
			}
		}

		// Generates a chart for the given temperature data
		public IActionResult GenerateChart(double temp, double tempMin, double tempMax)
		{
			var chartConfig = new
			{
				type = "bar",
				data = new
				{
					// Getting labels from the model, which gets them from the API
					labels = new[] { "Current", "Min", "Max" },
					datasets = new[] {
						new {
							label = "Temperature (°C)",
							data = new[] { temp, tempMin, tempMax },
							backgroundColor = new[] { "#4CAF50", "#2196F3", "#F44336" },
							borderWidth = 1
						}
					}
				},
				options = new
				{
					scales = new
					{
						y = new
						{
							beginAtZero = true
						}
					}
				}
			};

			var chartJson = JsonSerializer.Serialize(chartConfig);
			var chartUrl = $"https://quickchart.io/chart?c={Uri.EscapeDataString(chartJson)}";
			return Redirect(chartUrl);
		}

		// POST: /Weather/GetWeather
		// Retrieves weather information for the specified city
		[HttpPost]
		public async Task<IActionResult> GetWeather(string city)
		{
			try
			{
				var weather = await GetWeatherAsync(city);

				return View(weather);
			}
			catch (Exception ex)
			{
				TempData["Error"] = "Error fetching weather.";
				return RedirectToAction("Index");
			}
		}

		// Retrieves weather information from the OpenWeather API
		private async Task<WeatherModel> GetWeatherAsync(string city)
		{
			var client = _httpClientFactory.CreateClient();
			var apiKey = _configuration["OpenWeather:ApiKey"];
			var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&units=metric";

			var response = await client.GetFromJsonAsync<OpenWeatherResponse>(url);

			//Retunring model to the view
			return new WeatherModel
			{
				City = city,
				Temperature = response.Main.Temp,
				TempMin = response.Main.Temp_min,
				TempMax = response.Main.Temp_max,
				Humidity = response.Main.Humidity,
				Description = response.Weather.FirstOrDefault()?.Description ?? "No description" // if null return "No description"
			};
		}

		// Sends a weather alert via SMS using Twilio
		private async Task SendWeatherAlert(string phoneNumber, WeatherModel weather)
		{
			try
			{
				// Getting Twilio credentials from appsettings.json
				var accountSid = _configuration["Twilio:AccountSid"];
				var authToken = _configuration["Twilio:AuthToken"];
				var fromNumber = _configuration["Twilio:PhoneNumber"];

				TwilioClient.Init(accountSid, authToken);

				// Prepare the message
				var messageOptions = new CreateMessageOptions(
					new PhoneNumber("+1" + phoneNumber))
				{
					From = new PhoneNumber(fromNumber),
					Body = $"Weather Alert for {weather.City}: \n" +
						   $"High: {weather.TempMax}°C \n" +
						   $"Low: {weather.TempMin}°C \n" +
						   $"Humidity: {weather.Humidity}%\n" +
						   $"The weather is {weather.Description}"
				};
				// Send the message
				var message = await MessageResource.CreateAsync(messageOptions);
				Console.WriteLine($"Sent SMS SID: {message.Sid}");
			}
			catch (Exception ex)
			{
				throw new ApplicationException($"SMS failed: {ex.Message}", ex);
			}
		}

		// GET: /Weather/CompareWeather
		// Displays the compare weather view
		[HttpGet]
		public IActionResult CompareWeather()
		{
			return View();
		}

		// POST: /Weather/CompareWeather
		// Compares weather information for two cities
		[HttpPost]
		public async Task<IActionResult> CompareWeather(string city1, string city2)
		{
			try
			{
				var city1Data = await GetWeatherAsync(city1);
				var city2Data = await GetWeatherAsync(city2);

				// Tuple instead of CompareWeatherModel
				return View((city1Data, city2Data));
			}
			catch
			{
				TempData["Error"] = "Comparison failed";
				return View();
			}
		}

		// Generates a comparison chart for the given temperature data
		public IActionResult GenerateComparisonChart(string[] labels, double[] temps, double[] maxTemps, double[] minTemps)
		{
			var chartConfig = new
			{
				type = "bar",
				data = new
				{
					// Getting labels from the model, which gets them from the API
					labels = labels,
					datasets = new[]
					{
						new { label = "Current Temp (°C)", data = temps, backgroundColor = "#4CAF50" },
						new { label = "Max Temp (°C)", data = maxTemps, backgroundColor = "#2196F3" },
						new { label = "Min Temp (°C)", data = minTemps, backgroundColor = "#F44336" }
					}
				},
				options = new
				{
					responsive = true,
					scales = new
					{
						y = new { beginAtZero = true }
					}
				}
			};

			var chartJson = JsonSerializer.Serialize(chartConfig);
			var chartUrl = $"https://quickchart.io/chart?c={Uri.EscapeDataString(chartJson)}";
			return Redirect(chartUrl);
		}
	}
}

