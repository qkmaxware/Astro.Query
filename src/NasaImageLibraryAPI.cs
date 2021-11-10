/* 
TODO 
basic query is https://images-api.nasa.gov/search?q=M31
    {
        collection: {
            items: [
                {
                    data: [
                        keyword, media_type, title etc
                        ...
                    ],
                    href: "" !important!
                }
            ]
        }
    }
which href leads to https://images-assets.nasa.gov/image/PIA16682/collection.json
    [
        "i1.jpg", "i2.jpg", ..., "metadata.json"
    ]
where image urls can be found to download

more documentation found at https://api.nasa.gov/
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace Qkmaxware.Astro.Query {

public class NasaLibraryImage {
    public string Title {get; private set;}
    public string[] ImageUrls {get; private set;}

    public NasaLibraryImage(string title, string[] imageUrls) {
        this.Title = title;
        this.ImageUrls = imageUrls;
    }

    private bool downloadUrl(string url, string saveFilepath) {
        try{
            // Preserve extension
            var ext = Path.GetExtension(url);
            if (!saveFilepath.EndsWith(ext))
                saveFilepath += ext;

            // Create parent directories
            FileInfo info = new FileInfo(saveFilepath);
            info.Directory.Create();

            // Http request and download
            var cookies = new CookieContainer();
            using (var handler = new HttpClientHandler { CookieContainer = cookies, UseCookies = true })
            using (var client = new System.Net.Http.HttpClient(handler))  {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
                var response = client.GetStreamAsync(url);
                response.Wait();

                using (var fs = new FileStream(saveFilepath, FileMode.Create)){
                    var copyTask = response.Result.CopyToAsync(fs);
                    copyTask.Wait();
                }
            }
            return true;
        } catch {
            return false;
        }
        
    }
    private bool findCorrectUrlAndDownload(string filter, string saveFilepath) {
        if (this.ImageUrls == null)
            return false;
        
        foreach (var url in this.ImageUrls) {
            var name = Path.GetFileNameWithoutExtension(url);
            if (name.EndsWith(filter)) {
                return downloadUrl(url, saveFilepath);
            }
        }
        return false;
    }
    public bool DownloadOriginal(string saveFilepath) => findCorrectUrlAndDownload("~orig", saveFilepath);
    public bool DownloadLarge(string saveFilepath) => findCorrectUrlAndDownload("~large", saveFilepath);
    public bool DownloadMedium(string saveFilepath) => findCorrectUrlAndDownload("~medium", saveFilepath);
    public bool DownloadSmall(string saveFilepath) => findCorrectUrlAndDownload("~small", saveFilepath);
    public bool DownloadThumbnail(string saveFilepath) => findCorrectUrlAndDownload("~thumb", saveFilepath);
}

public static class NasaImageLibraryAPI {

    private static string baseUrl = "https://images-api.nasa.gov/search";

    public static IEnumerable<NasaLibraryImage> QueryImages(string query) {
        var builder = new UriBuilder(baseUrl);
        builder.AddParametre("q", query);
        builder.AddParametre("media_type", "image");

        var cookies = new CookieContainer();
        using (var handler = new HttpClientHandler { CookieContainer = cookies, UseCookies = true })
        using (var client = new System.Net.Http.HttpClient(handler))  {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");

            var task = client.GetStreamAsync(builder.Uri.ToString());
            task.Wait();
            var json = task.Result;
            var response = JsonSerializer.Deserialize<Response>(json);
            if (response != null && response.collection != null) {
                foreach (var item in response.collection.items) {
                    if (!string.IsNullOrEmpty(item?.href)) {
                        var getImageListTask = client.GetStringAsync(item.href);
                        getImageListTask.Wait();
                        var list = JsonSerializer.Deserialize<string[]>(getImageListTask.Result);

                        yield return new NasaLibraryImage(
                            title: item.data.FirstOrDefault()?.title,
                            imageUrls: list
                        );
                    }
                }
            }
        }
    }

    #region API Response 

    private class Response {
        public ResponseCollection collection {get; set;}
    }

    private class ResponseCollection {
        public string href {get; set;}
        public ResponseCollectionItems[] items {get; set;}
    }

    private class ResponseCollectionItems {
        public string href {get; set;}
        public ResponseCollectionData[] data {get; set;}
    }

    private class ResponseCollectionData {
        public string title {get; set;}
        public string href {get; set;}
    }

    #endregion

}

}