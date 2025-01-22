using Library;

var uri = $"https://elasticsearch.saintgimp.org/logstash-temperatures/_search";
var elasticSearchCredentials = Environment.GetEnvironmentVariable("ElasticSearchCredentials") ?? "";
string emailConnectionString = Environment.GetEnvironmentVariable("EmailConnectionString") ?? "";

try
{
    var mostRecentTemperature = await ElasticService.GetMostRecentDocument(uri, elasticSearchCredentials);

    if (mostRecentTemperature.Age > TimeSpan.FromMinutes(30))
    {
        EmailService.SendEmailNotification("Hey, I think the temperature sensors are offline!", emailConnectionString);
    }
    else
    {
        Console.WriteLine("Everything's fine here, we're all fine, how are you?");
    }
}
catch (Exception e)
{
    Console.WriteLine(e.ToString());
    EmailService.SendEmailNotification("I couldn't check on the temperature sensors!", emailConnectionString);
}
