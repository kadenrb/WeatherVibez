namespace VibeVaultC_.Models
// Default model for OpenWeather API response
{
	// Represents the response from the OpenWeather API
	public class OpenWeatherResponse
	{
		// Main weather data (temperature, humidity, etc.)
		public MainData? Main { get; set; }
		// Array of weather conditions (e.g., description of the weather)
		public WeatherData[]? Weather { get; set; }
	}

	// Represents the main weather data
	public class MainData
	{
		// Current temperature
		public double Temp { get; set; }
		// Minimum temperature
		public double Temp_min { get; set; }
		// Maximum temperature
		public double Temp_max { get; set; }
		// Humidity percentage
		public int Humidity { get; set; }
	}

	// Represents the weather condition data
	public class WeatherData
	{
		// Description of the weather (e.g., clear sky, rain)
		public string? Description { get; set; }
	}
}

