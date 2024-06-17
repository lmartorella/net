using Microsoft.Extensions.Configuration;

namespace Lucky.Garden.Device;

class Configuration(IConfiguration configuration)
{
    /// <summary>
    /// Used by MQTT for topic root
    /// </summary>
    public string DeviceId
    {
        get
        {
            // Mandatory
            return configuration["deviceId"]!;
        }
    }

    /// <summary>
    /// Used by REST. E.g. http://deviceName:port
    /// </summary>
    public string DeviceRest
    {
        get
        {
            // Mandatory
            return configuration["deviceRest"]!;
        }
    }
}
