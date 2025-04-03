namespace WeatherVibez.Models
{
	// Represents the weather data for a specific city
	public class WeatherModel
	{
		// Name of the city
		public string? City { get; set; }
		// Current temperature in Celsius
		public double Temperature { get; set; }
		// Feels like temperature in Celsius
		public double FeelsLike { get; set; }
		// Minimum temperature in Celsius
		public double TempMin { get; set; }
		// Maximum temperature in Celsius
		public double TempMax { get; set; }
		// Humidity percentage
		public int Humidity { get; set; }
		// Weather description (e.g. "clear sky")
		public string? Description { get; set; }
	}
}

