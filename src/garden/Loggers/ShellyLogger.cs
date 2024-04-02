using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lucky.Home 
{
    class ShellyLogger(ILogger<ShellyLogger> logger, IConfiguration configuration) : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Start");
            string var1 = configuration["var1"] ?? "<null>";
            logger.LogInformation("Conf, var1 {0}", var1);
            return Task.CompletedTask;
        }
    }
}