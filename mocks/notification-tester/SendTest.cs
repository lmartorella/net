using System.Text;
using Lucky.Home.Notification;
using Lucky.Home.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lucky.Tests;

class SendTest(ILogger<SendTest> logger, MqttService mqttService) : BackgroundService
{
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rpcCaller = await mqttService.RegisterRpcOriginator("notification/send_mail");

        for (int i = 0; i < 3; i++)
        {
            logger.LogInformation("Sending test mail {0}...", i);
            var task = rpcCaller.JsonRemoteCall<SendMailRequestMqttPayload, RpcVoid>(new SendMailRequestMqttPayload
                {
                    Title = "Test Mail",
                    Body = "Test Body",
                    IsAdminReport = true
                }
            );
            if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5))) != task)
            {
                logger.LogError("TIMEOUT");
                Environment.Exit(1);
            }
            await Task.Delay(TimeSpan.FromSeconds(5));
            logger.LogInformation("Mail sent");
        }
    }
}