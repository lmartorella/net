using Microsoft.Extensions.Configuration;

namespace Lucky.Home.Device 
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