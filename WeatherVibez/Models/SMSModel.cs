using System.ComponentModel.DataAnnotations;

namespace WeatherVibez.Models
{
	public class SMSModel
	{
		[Required]
		[Phone]
		public string ? PhoneNumber { get; set; }

		[Required]
		public string ? City { get; set; }
	}
}
