using Microsoft.Extensions.Configuration;

namespace Lucky.Garden.Device 
{
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
        /// Used by REST for DNS name
        /// </summary>
        public string DeviceName
        {
            get
            {
                // Mandatory
                return configuration["deviceName"]!;
            }
        }
    }
}