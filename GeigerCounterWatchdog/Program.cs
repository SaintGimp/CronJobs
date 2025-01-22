using Library;

var uri = $"https://elasticsearch.saintgimp.org/logstash-geiger/_search";
var elasticSearchCredentials = Environment.GetEnvironmentVariable("ElasticSearchCredentials") ?? "";
string emailConnectionString = Environment.GetEnvironmentVariable("EmailConnectionString") ?? "";

try
{
    var mostRecentReading = await ElasticService.GetMostRecentDocument(uri, elasticSearchCredentials);

    if (mostRecentReading.Age > TimeSpan.FromMinutes(30))
    {
        EmailService.SendEmailNotification("Hey, I think the geiger counter is offline!", emailConnectionString);
    }
    else if (mostRecentReading.Data["cpm"] > 256)
    {
        Console.WriteLine($"cpm is {mostRecentReading.Data["cpm"]}");
        EmailService.SendEmailNotification("Hey, I think the geiger counter is logging bad data!", emailConnectionString);
    }
    else
    {
        Console.WriteLine("Everything's fine here, we're all fine, how are you?");
    }
}
catch (Exception e)
{
    Console.WriteLine(e.ToString());
    EmailService.SendEmailNotification("I couldn't check on the geiger counter!", emailConnectionString);
}