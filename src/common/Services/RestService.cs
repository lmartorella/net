using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace Lucky.Home.Services;

public class RestService(ILogger<RestService> logger)
{
    private readonly HttpClient client = new HttpClient();

    /// <summary>
    /// Send args as JSON body
    /// </summary>
    public async Task<string> AsyncRest(string uri, HttpMethod method, string? jsonBody = null)
    {
        logger.LogInformation("Send {0} {1}", method, uri);
        var message = new HttpRequestMessage(method, uri);
        if (jsonBody != null)
        {
            message.Content = new StringContent(jsonBody, MediaTypeHeaderValue.Parse("application/json"));
        }
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
