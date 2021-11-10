using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Qkmaxware.Astro.IO.Tle;

namespace Qkmaxware.Astro.Query {

/// <summary>
/// Interface to the Celestrak API
/// </summary>
public class CelestrakAPI {
    public static readonly string ActiveSatellitesUrl = "https://celestrak.com/NORAD/elements/active.txt";
    public static readonly string SpaceStationsUrl = "https://celestrak.com/NORAD/elements/stations.txt";

    private static StreamReader downloadTleText(string url) {
        var cookies = new CookieContainer();
        using (var handler = new HttpClientHandler { CookieContainer = cookies, UseCookies = true })
        using (var client = new System.Net.Http.HttpClient(handler)) 
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");

            var task = client.GetStreamAsync(url);
            task.Wait();
            return new StreamReader(task.Result);
        }
    }

    private static IEnumerable<LineItem> readTle(string url) {
        var source = downloadTleText(url);
        var serializer = new TleDeserializer ();
        foreach (var lineItem in serializer.Deserialize(source)) {
            yield return lineItem;
        }
    }

    /// <summary>
    /// Query all space stations
    /// </summary>
    /// <returns>list of two line element items</returns>
    public static IEnumerable<LineItem> SpaceStations() {
        return readTle(SpaceStationsUrl);
    }

    /// <summary>
    /// Query all active satellites
    /// </summary>
    /// <returns>list of two line element items</returns>
    public static IEnumerable<LineItem> ActiveSatellites() {
        return readTle(ActiveSatellitesUrl);
    }
} 

}