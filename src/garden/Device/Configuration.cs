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
            var deviceId = configuration["deviceId"];
            if (deviceId == null)
            {
                throw new InvalidOperationException("Configuration error, missing deviceId");
            }
            return deviceId;
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
            var deviceUrl = configuration["deviceRest"]!;
            if (deviceUrl == null)
            {
                throw new InvalidOperationException("Configuration error, missing deviceUrl");
            }
            return deviceUrl;
        }
    }
}
