using System.Collections.Specialized;
using System.Web;
using Microsoft.Extensions.Logging;

namespace Lucky.Garden.Services;

public class RestService(ILogger<RestService> logger)
{
    private readonly HttpClient client = new HttpClient();

    private string EncodeQueryString(NameValueCollection args)
    {
        var qs = HttpUtility.ParseQueryString("");
        qs.Add(args);
        return qs.ToString()!;
    }

    public async Task<string> AsyncRest(Uri uri, HttpMethod method, NameValueCollection? args = null)
    {
        if (args != null)
        {
            var builder = new UriBuilder(uri)
            {
                Query = EncodeQueryString(args)
            };
            uri = builder.Uri;
        }
        logger.LogInformation("Send {0} {1}", method, uri);
        var message = new HttpRequestMessage(method, uri);
        var response = await client.SendAsync(message);
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            return await response.Content.ReadAsStringAsync();
        }
        else
        {
            throw new HttpRequestException(HttpRequestError.ResponseEnded, $"Error: {await response.Content.ReadAsStringAsync()}");
        }
    }
}
