using System.Text;
using Newtonsoft.Json.Linq;

namespace Library;

public static class ElasticService
{
    public static async Task<ElasticDocument> GetMostRecentDocument(string uri, string elasticSearchCredentials)
    {
        Console.WriteLine($"Loading data from ElasticSearch...");


        var httpClient = new HttpClient();
        var byteArray = Encoding.ASCII.GetBytes(elasticSearchCredentials);
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        var query = @"{
            ""query"": {
                ""match_all"": {}
            },
            ""size"": ""1"",
            ""sort"": [
                {
                ""@timestamp"": {
                    ""order"": ""desc""
                }
                }
            ]
            }";

        var response = await httpClient.PostAsync(uri, new StringContent(query, Encoding.ASCII, "application/json"));
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine(responseContent);

        dynamic data = JObject.Parse(responseContent);
        var document = new ElasticDocument(data.hits.hits[0]._source);
        Console.WriteLine($"Most recent timestamp is {document.Timestamp}, {document.Age} old");

        return document;
    }
}

public class ElasticDocument
{
    public ElasticDocument(dynamic data) => Data = data;

    public dynamic Data { get; }
    public DateTime Timestamp => Data["@timestamp"];
    public TimeSpan Age => DateTime.UtcNow - Timestamp;
}
