using Newtonsoft.Json;
using RestSharp;

namespace Abraham.OpenWeatherMap;

/// <summary>
/// Data access logic for OpenWeatherMap Web API 
/// </summary>
public class OpenWeatherMapConnector
{
    #region ------------- Fields --------------------------------------------------------------
    private string         _apiUrl       = "https://api.openweathermap.org/data/3.0/onecall";
    private string         _tempFileName = "saved_weather_forecast.json";
    private Action<string> _logger       = delegate(string logMessage){ };
    private string         _myApiKey     = "";
    private string         _lattitude    = "";
    private string         _longitude    = "";
    private string         _language     = "de";
    private string         _units        = "metric";
    #endregion



    #region ------------- Init ----------------------------------------------------------------
    public OpenWeatherMapConnector()
    {
    }
    #endregion



    #region ------------- Methods -------------------------------------------------------------
    /// <summary>
    /// Sets the URL of the OpenWeatherMap API
    /// </summary>
    public OpenWeatherMapConnector UseUrl(string apiUrl)
	{
		_apiUrl = apiUrl ?? throw new ArgumentNullException(nameof(apiUrl));
		return this;
	}

    public OpenWeatherMapConnector UseApiKey(string myApiKey)
    {
        if (string.IsNullOrWhiteSpace(myApiKey))
            throw new ArgumentNullException(nameof(myApiKey));
        _myApiKey = myApiKey;
		return this;
    }

    public OpenWeatherMapConnector UseLocation(string lattitude, string longitude)
    {
        if (string.IsNullOrWhiteSpace(lattitude))
            throw new ArgumentNullException(nameof(lattitude));
        if (string.IsNullOrWhiteSpace(longitude))
            throw new ArgumentNullException(nameof(longitude));
        _lattitude = lattitude;
        _longitude = longitude;
		return this;
    }

    /// <summary>
    /// Sets the name of the temp file, where the current forecast is saved.
    /// This must point to a valid filename, a path can be included.
    /// </summary>
	public OpenWeatherMapConnector UseTempFile(string tempFileName)
	{
		_tempFileName = tempFileName ?? throw new ArgumentNullException(nameof(tempFileName));
		return this;
	}

    /// <summary>
    /// Sets the logger, which is called for every log message.
    /// </summary>
    /// <example>
    /// UseLogger(Console.WriteLine);
    /// </example>
    public OpenWeatherMapConnector UseLogger(Action<string> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        return this;
    }

    public OpenWeatherMapConnector UseLanguage(string language)
    {
        _language = language ?? throw new ArgumentNullException(nameof(language));
        return this;
    }

    public OpenWeatherMapConnector UseUnits(string units)
    {
        _units = units ?? throw new ArgumentNullException(nameof(units));
        return this;
    }

    /// <summary>
    /// Reads the current temperature and the forecast for the next 24 hours.
    /// </summary>
    /// <param name="currentTime">You can omit this parameter. DateTime.Now will be used.</param>
    public WeatherInfo ReadCurrentTemperatureAndForecast(DateTime? currentTime = null)
    {
        if (string.IsNullOrWhiteSpace(_apiUrl))
            throw new Exception($"The URL of the Web API must be set. Please call Method '{nameof(UseUrl)}' first, before calling this method.");
        if (string.IsNullOrWhiteSpace(_tempFileName))
            throw new Exception($"The name of the temp file must be set. Please call Method '{nameof(UseTempFile)}' first, before calling this method.");
        if (string.IsNullOrWhiteSpace(_myApiKey))
            throw new Exception($"The API key must be set. Please call Method '{nameof(UseApiKey)}' first, before calling this method.");
        if (string.IsNullOrWhiteSpace(_lattitude))
            throw new Exception($"The name of the temp file must be set. Please call Method '{nameof(UseLocation)}' first, before calling this method.");
        if (string.IsNullOrWhiteSpace(_longitude))
            throw new Exception($"The name of the temp file must be set. Please call Method '{nameof(UseLocation)}' first, before calling this method.");

        DateTime now = currentTime ?? DateTime.Now;

        var weather = GetCurrentWeather();

        AddTimesForUnixTimestampsInList(weather, now);

        // to have the whole day at hand, we save the current forecast every morning
        // later on that day we take the saved state to build the forecast object
        if (now.TimeOfDay >= new TimeSpan(5, 0, 0) &&
            now.TimeOfDay < new TimeSpan(7, 0, 0))
            Save(weather);
        var savedWeather = LoadSavedWeather();


        var forecast = new List<Forecast>();
        var today = now.Date;
        forecast.Add(FindEntry(weather, savedWeather, today.AddHours(7)));  // today at 7:00
        forecast.Add(FindEntry(weather, savedWeather, today.AddHours(13))); // today at 13:00
        forecast.Add(FindEntry(weather, savedWeather, today.AddHours(19))); // today at 19:00
        forecast.Add(FindEntry(weather, savedWeather, today.AddHours(23))); // today at 23:00

        var temperature = FindCurrentTemperature(weather);
        temperature = Math.Round(temperature, 0);
        
        return new WeatherInfo(temperature, "°C", forecast, weather, savedWeather);
    }

    public Forecast FindForecastEntryByTime(WeatherInfo weatherInfo, int hour)
    {
        if (weatherInfo is null)
            throw new ArgumentException($"The parameter '{nameof(weatherInfo)}' cannot be null.");
        if (hour < 0 || hour > 23)
            throw new ArgumentException($"The parameter '{nameof(hour)}' must be between 0 and 23.");

        var pointOfTime = DateTime.Now.Date.AddHours(hour);
        return FindEntry(weatherInfo.Model, weatherInfo.SavedModel, pointOfTime);
    }

    public string GetUniCodeSymbolForWeatherIcon(WeatherIcon icon)
    {
        switch (icon)
        {
            case WeatherIcon.Cloud:               return char.ConvertFromUtf32(0x2601);
            case WeatherIcon.CloudWithLightning:  return char.ConvertFromUtf32(0x26C8);
            case WeatherIcon.CloudWithRain:       return char.ConvertFromUtf32(0x2614); // 0x1F327);
            case WeatherIcon.CloudWithSnow:       return char.ConvertFromUtf32(0x2603); // 0x1F328);
            case WeatherIcon.MediumCloud:         return char.ConvertFromUtf32(0x26C5);
            case WeatherIcon.SmallCloud:          return char.ConvertFromUtf32(0x26C5); // 0x1F324);
            case WeatherIcon.Sun:                 return char.ConvertFromUtf32(0x2600);
            case WeatherIcon.SunCloudRain:        return char.ConvertFromUtf32(0x26C5); // 0x1F326);
            case WeatherIcon.ThunderCloudAndRain: return char.ConvertFromUtf32(0x26C8);
            case WeatherIcon.Moon:                return char.ConvertFromUtf32(0x263D); // 0x1F319);
            case WeatherIcon.Snow:                return char.ConvertFromUtf32(0x2603);
            case WeatherIcon.Fog:                 return char.ConvertFromUtf32(0x2601); // 0x1F32B);
            case WeatherIcon.Unknown: default:    return char.ConvertFromUtf32(0x26C4);                
        }
    }
    #endregion



    #region ------------- Implementation ------------------------------------------------------
    private WeatherModel GetCurrentWeather()
    {
        //example:
        //"https://api.openweathermap.org/data/3.0/onecall?lat=53.8667&lon=9.8833&lang=de&units=metric&appid=00000000000000000000000000000000";
        var requestUrl = $"{_apiUrl}?lat={_lattitude}&lon={_longitude}&lang={_language}&units={_units}&appid={_myApiKey}&";

        var httpClient = new RestClient();
        var request = new RestRequest(requestUrl, Method.Get);
        var response = httpClient.Execute(request);
        if (response is null)
        {
            _logger($"Request wasn't successful.");
            return new WeatherModel();
        }
        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            _logger($"Request wasn't successful.\nMore info:{response.StatusDescription}");
            return new WeatherModel();
        }
        if (response.Content is null)
        {
            _logger($"Request wasn't successful.\nMore info:{response.StatusDescription}");
            return new WeatherModel();
        }

        var myWeatherData = JsonConvert.DeserializeObject<WeatherModel>(response.Content);
        if (myWeatherData is null)
        {
            _logger($"Data cannot be deserialized");
            return new WeatherModel();
        }

        return myWeatherData;
    }

    private void AddTimesForUnixTimestampsInList(WeatherModel weather, DateTime now)
    {
        foreach (var hour in weather.hourly)
            hour.Time = ConvertUnixTimeToDateTime(hour.dt, now);
    }

    private double FindCurrentTemperature(WeatherModel weather)
    {
        return weather.current.temp;
    }

    private Forecast FindEntry(WeatherModel currentWeather, WeatherModel? historicWeather, DateTime dateTime)
    {
        _logger($"{dateTime}: ");
        var result = new Forecast();
        result.Temp = 0.0;

        var entry = FindEntryForNow(currentWeather, dateTime);

        // If it is later than 7:00 am, we won't find earlier entries in "currentWeather".
        // in this cast we look into te saved state from 5:00 am
        if (entry is null && historicWeather is not null)
        {
            entry = FindEntryForNow(historicWeather, dateTime);
            if (entry is null)
                _logger($"    historic data not found!");
            else
                _logger($"    taking historic data    ");
        }

        // If we also didn't find it there, we give up
        if (entry is null)
        {
            _logger($"    no data!");
            return result;
        }

        // remove the timezone
        result.Hour = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second);
        result.Temp = Math.Round(entry.temp, 0);

        var icon = entry.weather.First();
        if (icon is null)
        {
            _logger($"no icon!    ");
        }
        else
        {
            result.Icon = ConvertWeatherIcon(icon.id.ToString());
            result.IconDescription = icon.description;
            result.WeatherDescription = icon.main;
            result.UnicodeSymbol = GetUniCodeSymbolForWeatherIcon(result.Icon);
        }

        _logger($"Hour: {result.Hour}, Temp: {result.Temp}  Icon: {icon?.id} {result.Icon.ToString("G")} IconDesc: {result.IconDescription} Desc: {result.WeatherDescription}");
        return result;
    }

    private static Hourly? FindEntryForNow(WeatherModel currentWeather, DateTime dateTime)
    {
        return currentWeather.hourly
            .Where(x => TimeIsToday(x.Time, dateTime) && TimeIsInThatHour(x.Time, dateTime))
            .FirstOrDefault();
    }

    private static bool TimeIsInThatHour(DateTimeOffset x, DateTime timeNow)
    {
        var now = timeNow.TimeOfDay;
        var nowPlusOneHour = timeNow.AddHours(1).AddSeconds(-1).TimeOfDay;
        return x.TimeOfDay >= now && x.TimeOfDay < nowPlusOneHour;
    }

    private static bool TimeIsToday(DateTimeOffset x, DateTime timeNow)
    {
        return x.Date == timeNow.Date;
    }

    private DateTimeOffset ConvertUnixTimeToDateTime(int epoch, DateTime now)
    {
        var timeZone = TimeZone.CurrentTimeZone; 
        var timezoneOffset = timeZone.GetUtcOffset(now);
        return new DateTimeOffset(1970, 1, 1, 0, 0, 0, timezoneOffset).AddSeconds(epoch);
    }

    private WeatherIcon ConvertWeatherIcon(string icon)
    {
        if (icon is null)           return WeatherIcon.Unknown;
        if (icon.StartsWith("2"))   return WeatherIcon.ThunderCloudAndRain;
        if (icon.StartsWith("3"))   return WeatherIcon.SunCloudRain;
        if (icon.StartsWith("50"))  return WeatherIcon.SunCloudRain;
        if (icon.StartsWith("51"))  return WeatherIcon.CloudWithSnow;
        if (icon.StartsWith("52"))  return WeatherIcon.CloudWithRain;
        if (icon.StartsWith("5"))   return WeatherIcon.CloudWithRain;
        if (icon.StartsWith("6"))   return WeatherIcon.Snow;
        if (icon.StartsWith("7"))   return WeatherIcon.Fog;
        if (icon == "800")          return WeatherIcon.Sun;
        if (icon.StartsWith("8"))   return WeatherIcon.Cloud;
        return WeatherIcon.Unknown;
    }

    private WeatherModel? LoadSavedWeather()
    {
        if (!File.Exists(_tempFileName))
            return null;
        var serializedWeatherModel = File.ReadAllText(_tempFileName);
        if (serializedWeatherModel is null)
            return null;

        return JsonConvert.DeserializeObject<WeatherModel>(serializedWeatherModel);
    }

    private void Save(WeatherModel weather)
    {
        var serializedWeatherModel = JsonConvert.SerializeObject(weather);
        File.WriteAllText(_tempFileName, serializedWeatherModel);
    }
    #endregion
}
