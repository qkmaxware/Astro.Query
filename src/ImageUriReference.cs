using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Qkmaxware.Astro.Query {

/// <summary>
/// A reference to an image available at the given URI
/// </summary>
public class ImageUriReference {
    private string uri;
    public ImageUriReference(string uri) {
        this.uri = uri;
    }

    protected static bool DownloadUriToStream(string uri, Stream writeTo) {
        try{
            // Http request and download
            var cookies = new CookieContainer();
            using (var handler = new HttpClientHandler { CookieContainer = cookies, UseCookies = true })
            using (var client = new System.Net.Http.HttpClient(handler))  {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
                var response = client.GetStreamAsync(uri);
                response.Wait();
                
                var copyTask = response.Result.CopyToAsync(writeTo);
                copyTask.Wait();
            }
            return true;
        } catch {
            return false;
        }
    }

    public bool DownloadImage(Stream stream) {
        return DownloadUriToStream(this.uri.ToString(), stream);
    }
    public bool DownloadImageToFile(string filepath) {
        // Create parent directories
        FileInfo info = new FileInfo(filepath);
        info.Directory.Create();

        using (var fs = new FileStream(filepath, FileMode.Create)) {
            return DownloadUriToStream(this.uri.ToString(), fs);
        }
    }
}

}