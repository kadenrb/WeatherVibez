using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using WeatherVibez.Models;

namespace WeatherVibez.Controllers
{
	[Route("Payments")]
	[Authorize]
	public class PaymentsController : Controller
	{
		private readonly IConfiguration _config;
		private readonly UserManager<User> _userManager;

		// Constructor to initialize UserManager and IConfiguration, and set the Stripe API key
		public PaymentsController(UserManager<User> userManager, IConfiguration config)
		{
			_config = config;
			_userManager = userManager;
			StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
		}

		// GET: /Payments/Balance
		// Retrieves the balance for the authenticated user and creates a Stripe customer if needed
		[HttpGet("Balance")]
		public async Task<IActionResult> Balance()
		{
			var user = await _userManager.GetUserAsync(User);

			// Create Stripe customer if needed
			if (string.IsNullOrEmpty(user.StripeCustomerId))
			{
				var options = new CustomerCreateOptions
				{
					Email = user.Email,
					Metadata = new Dictionary<string, string> { { "UserId", user.Id } }
				};

				var service = new CustomerService();
				var customer = await service.CreateAsync(options);
				user.StripeCustomerId = customer.Id;
				await _userManager.UpdateAsync(user);
			}

			return View(user);
		}

		// Tiny model to represent a charge request
		public class ChargeRequest
		{
			public decimal Amount { get; set; }
		}

		// POST: /Payments/Charge
		// Processes a charge request for the authenticated user
		[HttpPost("Charge")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Charge([FromBody] ChargeRequest request)
		{
			try
			{
				var user = await _userManager.GetUserAsync(User);

				if (user == null)
				{
					return Unauthorized(new { error = "Not authenticated" });
				}

				// Convert amount from dollars to cents
				var paymentIntentAmount = (long)(request.Amount * 100);

				var options = new PaymentIntentCreateOptions
				{
					//Creating a payment intent with the srtipe API
					Amount = paymentIntentAmount,
					Currency = "cad",
					Customer = user.StripeCustomerId,
					PaymentMethodTypes = new List<string> { "card" },
					Metadata = new Dictionary<string, string> { { "UserId", user.Id } }
				};

				var service = new PaymentIntentService();
				var paymentIntent = await service.CreateAsync(options);

				return Json(new
				{
					clientSecret = paymentIntent.ClientSecret,
					amount = request.Amount // Already in dollars
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine($"ERROR: {ex}");
				return StatusCode(500, new { error = ex.Message });
			}
		}

		// Tiny model to represent a confirm payment request
		public class ConfirmPaymentRequest
		{
			public decimal Amount { get; set; }
		}

		// POST: /Payments/ConfirmPayment
		// Confirms a payment and updates the user's balance
		[HttpPost("ConfirmPayment")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
		{
			var user = await _userManager.GetUserAsync(User);
			user.Balance += request.Amount;
			await _userManager.UpdateAsync(user);
			return Json(new { newBalance = user.Balance });
		}

		// GET: /Payments/PaymentSuccess
		// Displays the payment success view
		public IActionResult PaymentSuccess()
		{
			return View();
		}
	}
}
