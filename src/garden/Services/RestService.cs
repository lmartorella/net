using Microsoft.Extensions.Logging;

namespace Lucky.Garden.Services;

public class RestService(ILogger<RestService> logger)
{
    private readonly HttpClient client = new HttpClient();

    /// <summary>
    /// Send args as JSON body
    /// </summary>
    public async Task<string> AsyncRest(string uri, HttpMethod method, string? body = null)
    {
        logger.LogInformation("Send {0} {1}", method, uri);
        var message = new HttpRequestMessage(method, uri);
        if (body != null)
        {
            message.Content = new StringContent(body);
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
