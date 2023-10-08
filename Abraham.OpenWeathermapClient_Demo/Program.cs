using Abraham.OpenWeatherMap;

namespace Abraham.OpenWeatherMap_Demo
{
    /// <summary>
    /// Demo of the Nuget package.
    /// Demonstrates how to read the current temperature.
    /// 
    /// Author:
    /// Oliver Abraham, mail@oliver-abraham.de, https://www.oliver-abraham.de
    /// 
    /// Source code hosted at: 
    /// https://github.com/OliverAbraham/Abraham.OpenWeatherMapClient
    /// 
    /// Nuget Package hosted at: 
    /// https://www.nuget.org/packages/Abraham.OpenWeatherMapClient/
    /// 
    /// </summary>
    /// 
    internal class Program
    {
        // Weather in northern Germany
        private static string _myApiKey = "ENTER YOUR API KEY HERE - YOU'LL GET ONE FOR FREE AT https://openweathermap.org/api";

        static void Main()
        {
            Console.WriteLine("Demo for the Nuget package 'Abraham.OpenWeatherMapClient'");

            var client = new OpenWeatherMapConnector()
                .UseApiKey(_myApiKey)
                .UseLocation(lattitude:"53.8667", longitude:"9.8833");

            var weatherInfo = client.ReadCurrentTemperatureAndForecast();

            Console.WriteLine($"Current temperature in Berlin: {weatherInfo.CurrentTemperature} {weatherInfo.Unit}");
        }
    }
}
