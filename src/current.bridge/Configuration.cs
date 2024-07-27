using Microsoft.Extensions.Configuration;

namespace Lucky.Home.Services;

public class Configuration(IConfiguration configuration)
{
    public string AmmeterHostName
    {
        get
        {
            return configuration["CurrentSensorHostName"] ?? "current-sensor";
        }
    }

    public int AmmeterStationId
    {
        get
        {
            return int.Parse(configuration["CurrentSensorStationId"] ?? "1");
        }
    }
}
