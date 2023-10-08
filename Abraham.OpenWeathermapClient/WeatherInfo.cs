namespace Abraham.OpenWeatherMap
{
    public class WeatherInfo
    {
        public double CurrentTemperature { get; }
        public string Unit { get; }
        public List<Forecast> SmallForecast { get; }
        public WeatherModel Model { get; }
        public WeatherModel SavedModel { get; }

        public WeatherInfo(double currentTemperature, string unit, List<Forecast> smallForecast, WeatherModel model, WeatherModel savedModel)
        {
            CurrentTemperature = currentTemperature;
            Unit = unit;
            SmallForecast = smallForecast;
            Model = model;
            SavedModel = savedModel;
        }
    }
}
