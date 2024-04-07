using Microsoft.Extensions.Configuration;

namespace Lucky.Garden.Device 
{
    class Configuration(IConfiguration configuration) {
        public string DeviceId
        {
            get
            {
                // Mandatory
                return configuration["deviceId"]!;
            }
        }
    }
}