﻿using Library;
using System.Net.Sockets;
using System.Text;

var uri = $"https://elasticsearch.saintgimp.org/logstash-temperatures/_search";
var elasticSearchCredentials = Environment.GetEnvironmentVariable("ElasticSearchCredentials") ?? "";

try
{
    var mostRecentTemperature = await ElasticService.GetMostRecentDocument(uri, elasticSearchCredentials);

    if (mostRecentTemperature.Age > TimeSpan.FromMinutes(30) || mostRecentTemperature.Data.t3 == null)
    {
        Console.WriteLine("Didn't get a temperature from the weather station");
        return;
    }

    var temperature = (int)Math.Round((double)mostRecentTemperature.Data.t3 * 9.0 / 5.0 + 32.0);
    Console.WriteLine($"Temperature is {temperature} F");

    var packet = new AprsWeatherDataPacket(
        accountNumber: "EW9714",
        equipmentIdentifier: "custom",
        latitudeInDegrees: 47.697201f,
        longitudeInDegrees: -122.063844f,
        temperatureInFahrenheit: temperature);

    await SendDataToCwop(packet);

    Console.WriteLine("Everything's fine here, we're all fine, how are you?");
}
catch (Exception e)
{
    Console.WriteLine(e.ToString());
}

async Task SendDataToCwop(AprsWeatherDataPacket packet)
{
    using var client = new TcpClient("cwop.aprs.net", 14580);
    using var stream = client.GetStream();
    
    await Send("user EW9714 pass -1 vers custom 1.00\r\n", stream);
    await ReceiveResponse(stream);
    
    await Task.Delay(TimeSpan.FromSeconds(3));

    await Send(packet.ToString(), stream);
    await Task.Delay(TimeSpan.FromSeconds(3));
}

async Task Send(string message, NetworkStream stream)
{
    Console.WriteLine($"Sending: {message}");
    byte[] data = Encoding.ASCII.GetBytes(message);
    await stream.WriteAsync(data);
}

async Task ReceiveResponse(NetworkStream stream)
{
    var data = new byte[256];
    int numberOfBytes = await stream.ReadAsync(data);
    var responseData = Encoding.ASCII.GetString(data, 0, numberOfBytes);
    Console.WriteLine($"Received: {responseData}");
}

public class AprsWeatherDataPacket
{
    // http://weather.gladstonefamily.net/aprswxnet.html
    public string AccountNumber { get; private set; }
    public float LatitudeInDegrees { get; private set; }
    public float LongitudeInDegrees { get; private set; }
    public int? WindDirectionInDegrees { get; private set; }
    public int? WindSpeedInMph { get; private set; }
    public int? MaximumGustSpeedInMph { get; private set; }
    public int? TemperatureInFahrenheit { get; private set; }
    public float? Rainfall1HourInInches { get; private set; }
    public float? Rainfall24HoursInInches { get; private set; }
    public float? RainfallSinceMidnightInInches { get; private set; }
    public int? PercentHumidity { get; private set; }
    public float? AdjustedPressureInMillibars { get; private set; }
    public string EquipmentIdentifier { get; private set; }

    public AprsWeatherDataPacket(
        string accountNumber,
        string equipmentIdentifier,
        float latitudeInDegrees,
        float longitudeInDegrees,
        int? windDirectionInDegrees = null,
        int? windSpeedInMph = null,
        int? maximumGustSpeedInMph = null,
        int? temperatureInFahrenheit = null,
        float? rainfall1HourInInches = null,
        float? rainfall24HoursInInches = null,
        float? rainfallSinceMidnightInInches = null,
        int? percentHumidity = null,
        float? adjustedPressureInMillibars = null
        )
    {
        AccountNumber = accountNumber;
        EquipmentIdentifier = equipmentIdentifier;
        LatitudeInDegrees = latitudeInDegrees;
        LongitudeInDegrees = longitudeInDegrees;
        WindDirectionInDegrees = windDirectionInDegrees;
        WindSpeedInMph = windSpeedInMph;
        MaximumGustSpeedInMph = maximumGustSpeedInMph;
        TemperatureInFahrenheit = temperatureInFahrenheit;
        Rainfall1HourInInches = rainfall1HourInInches;
        Rainfall24HoursInInches = rainfall24HoursInInches;
        RainfallSinceMidnightInInches = rainfallSinceMidnightInInches;
        PercentHumidity = percentHumidity;
        AdjustedPressureInMillibars = adjustedPressureInMillibars;
    }

    public override string ToString()
    {
        return $"{AccountNumber}>APRS,TCPIP*:/{TimeString()}{LocationString()}{WindDirectionString()}{WindSpeedString()}{GustWindSpeedString()}{TemperatureString()}{OneHourRainString()}{TwentyFourRainString()}{RainSinceMidnightString()}{HumidityString()}{PressureString()}e{EquipmentIdentifier}\r\n";
    }

    private string TimeString()
    {
        var now = DateTime.UtcNow;
        return $"{now.Day:D2}{now.Hour:D2}{now.Minute:D2}z";
    }

    private string LocationString()
    {
        return $"{LatitudeString()}/{LongitudeString()}";
    }
    private string LatitudeString()
    {
        var absoluteLatitude = Math.Abs(LatitudeInDegrees);
        var degrees = (int)absoluteLatitude;
        var minutes = DecimalPart(absoluteLatitude) * 60m;
        var direction = LatitudeInDegrees > 0 ? "N" : "S";
        return $"{degrees:D2}{minutes:00.00}{direction}";
    }

    private string LongitudeString()
    {
        var absoluteLongitude = Math.Abs(LongitudeInDegrees);
        var degrees = (int)absoluteLongitude;
        var minutes = DecimalPart(absoluteLongitude) * 60m;
        var direction = LongitudeInDegrees > 0 ? "E" : "W";
        return $"{degrees:D3}{minutes:00.00}{direction}";
    }

    private decimal DecimalPart(float number)
    {
        return (decimal)number % 1;
    }

    private string WindDirectionString()
    {
        if (!WindDirectionInDegrees.HasValue)
        {
            return "_...";
        }

        var safeDirection = WindDirectionInDegrees % 360;
        if (safeDirection < 0)
        {
            safeDirection += 360;
        }
        return $"_{safeDirection:000}";
    }

    private string WindSpeedString()
    {
        return $"/{WindString(WindSpeedInMph)}";
    }

    private string WindString(int? speedInMph)
    {
        if (!WindSpeedInMph.HasValue)
        {
            return "...";
        }

        var safeSpeed = Math.Min(WindSpeedInMph.Value, 999);
        safeSpeed = Math.Max(safeSpeed, 0);
        return $"{safeSpeed:000}";
    }

    private string GustWindSpeedString()
    {
        return $"g{WindString(MaximumGustSpeedInMph)}";
    }

    private string TemperatureString()
    {
        if (!TemperatureInFahrenheit.HasValue)
        {
            return "t...";
        }

        var safeTemperature = Math.Max(TemperatureInFahrenheit.Value, -99);
        safeTemperature = Math.Min(safeTemperature, 999);
        return $"t{safeTemperature:000;-00}";
    }

    private string OneHourRainString()
    {
        return Rainfall1HourInInches.HasValue ? $"r{RainString(Rainfall1HourInInches)}" : "";
    }

    private string TwentyFourRainString()
    {
        return Rainfall24HoursInInches.HasValue ? $"p{RainString(Rainfall24HoursInInches)}" : "";
    }

    private string RainSinceMidnightString()
    {
        return RainfallSinceMidnightInInches.HasValue ? $"P{RainString(RainfallSinceMidnightInInches)}" : "";
    }

    private string RainString(float? rainInInches)
    {
        if (!rainInInches.HasValue)
        {
            return "...";
        }

        var rainInHundredths = (int)(rainInInches * 100);
        rainInHundredths = Math.Min(rainInHundredths, 999);
        return $"{rainInHundredths:000}";
    }

    private string HumidityString()
    {
        if (!PercentHumidity.HasValue)
        {
            return "";
        }

        var safeHumidity = Math.Max(PercentHumidity.Value, 1);
        safeHumidity = safeHumidity <= 99 ? safeHumidity : 0;
        return $"h{safeHumidity:00}";
    }

    private string PressureString()
    {
        if (!AdjustedPressureInMillibars.HasValue)
        {
            return "";
        }

        var tenthsOfMillibars = (int)(AdjustedPressureInMillibars * 10);
        var safeValue = Math.Max(0, tenthsOfMillibars);
        safeValue = Math.Min(safeValue, 99999);
        return $"b{safeValue:00000}";
    }
}
