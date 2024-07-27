using Microsoft.Extensions.Configuration;

namespace Lucky.Home.Services;

public class Configuration(IConfiguration configuration)
{
    public string InverterHostName
    {
        get
        {
            return configuration["InverterHostName"] ?? "sofar";
        }
    }

    public int InverterStationId
    {
        get
        {
            return int.Parse(configuration["InverterStationId"] ?? "1");
        }
    }
}
