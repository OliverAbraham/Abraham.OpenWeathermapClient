namespace Abraham.OpenWeatherMap
{
    public class WeatherInfo
    {
        public double CurrentTemperature { get; }
        public string Unit { get; }
        public List<Forecast> Forecast { get; }

        public WeatherInfo(double currentTemperature, string unit, List<Forecast> forecast)
        {
            CurrentTemperature = currentTemperature;
            Unit = unit;
            Forecast = forecast;
        }
    }
}
