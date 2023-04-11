using System.Net;
using System.Net.Http.Json;
using Mono.Options;
using Newtonsoft.Json;

string host = null; // "192.168.1.3:8989";
string apiKey = null;
string file = null;
string logFile = AppContext.BaseDirectory + "SonarrRefresh.log";

OptionSet options = null;
options = new OptionSet() {
    { "Sonarr Refresh Show Utility" },
    { "h|?|help", "Show this help", v => ShowHelp() },
    { "k|api_key=", "The Sonarr API key", v => apiKey = v },
    { "f|file=", "The file to search and refresh for", v => file = v },
    { "l|log=", "The file path to write log message", v => logFile = v },
    { "host=", "The host:port of Sonarr", v => host = v },
};
options.Parse(args);

Log("");

try
{
    if (String.IsNullOrWhiteSpace(file) || !File.Exists(file))
        throw new Exception("Missing search file. Must provide arg and verify file exists");

    if (String.IsNullOrWhiteSpace(apiKey))
        throw new Exception("Missing api key");

    if (String.IsNullOrWhiteSpace(host))
        throw new Exception("Missing Sonarr host argument");

    Log("Retrieving series list from Sonarr");
    var seriesList = await ApiGet<SonarrShow[]>("/api/series");
    Log($"Retrieved {seriesList.Length} shows successfully.");

    Log($"Searching for '{file}' in Sonarr");

    foreach (var f in seriesList)
    {
        if (file.StartsWith(f.path, StringComparison.InvariantCultureIgnoreCase))
        {
            Log("Found matching series: " + f.title + ". Triggering refresh...");
            var refreshResp = await ApiPost<RefreshSeriesRequest, CommandRegisteredResponse>("/api/v3/command", new RefreshSeriesRequest { seriesId = f.id });
            Log("Success: " + refreshResp.status);
            Environment.Exit(0);
        }
    }

    Log("Error: Unable to find any matching series in Sonarr containing this file. Quitting.");
}
catch (Exception ex)
{
    Log("A fatal error has occurred.");
    Log("");
    Log(ex.ToString());
    Log("Exited.");
}

void Log(string msg)
{
    Console.WriteLine(msg);
    if (!String.IsNullOrWhiteSpace(logFile))
        File.AppendAllText(logFile, Environment.NewLine + $"[{DateTime.Now.ToString("f")}] " + msg);
}

void ShowHelp()
{
    Console.WriteLine(options.GetHelpText());
    Environment.Exit(0);
}

async Task<T> ApiGet<T>(string url)
{
    using var handler = new HttpClientHandler()
    {
        AllowAutoRedirect = true,
        AutomaticDecompression = DecompressionMethods.Deflate,
    };
    using var hc = new HttpClient(handler);
    var req = new HttpRequestMessage(HttpMethod.Get, $"http://{host}{url}");
    req.Headers.Add("X-Api-Key", apiKey);
    var resp = hc.Send(req);
    var json = await resp.Content.ReadAsStringAsync();
    return JsonConvert.DeserializeObject<T>(json);
}

async Task<TResp> ApiPost<TReq, TResp>(string url, TReq jsonReq)
{
    using var handler = new HttpClientHandler()
    {
        AllowAutoRedirect = true,
        AutomaticDecompression = DecompressionMethods.Deflate,
    };
    using var hc = new HttpClient(handler);
    var req = new HttpRequestMessage(HttpMethod.Post, $"http://{host}{url}");
    req.Content = JsonContent.Create(jsonReq);
    req.Headers.Add("X-Api-Key", apiKey);
    var resp = hc.Send(req);
    var json = await resp.Content.ReadAsStringAsync();
    return JsonConvert.DeserializeObject<TResp>(json);
}

class SonarrShow
{
    public string title { get; set; }
    public string path { get; set; }
    public int id { get; set; }
}

class RefreshSeriesRequest
{
    public string name { get; set; } = "RefreshSeries";
    public int seriesId { get; set; }
}

class CommandRegisteredResponse
{
    public int id { get; set; }
    public string status { get; set; }
}