using Microsoft.Extensions.Configuration;

namespace Lucky.Home.Services;

public class Configuration(IConfiguration configuration)
{
    public string CurrentSensorHostName
    {
        get
        {
            return configuration["CurrentSensorHostName"] ?? "current-sensor";
        }
    }

    public int CurrentSensorStationId
    {
        get
        {
            return int.Parse(configuration["CurrentSensorStationId"] ?? "1");
        }
    }
}
