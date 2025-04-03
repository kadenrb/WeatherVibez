using WeatherVibez.Models;

namespace WeatherVibez.ViewModels
{
	// ViewModel for comparing weather data between two cities
	public class CityComparison
	{
		// Weather data for the first city
		public WeatherModel? City1 { get; set; }
		// Weather data for the second city
		public WeatherModel? City2 { get; set; }

		// Chart data for the comparison
		// Labels for the chart (e.g., "Current", "Min", "Max")
		public string[]? ChartLabels { get; set; }
		// Temperature data for the first city
		public double[]? City1Temperatures { get; set; }
		// Temperature data for the second city
		public double[]? City2Temperatures { get; set; }
	}
}

