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
        await mqttService.WaitConnected();

        for (int i = 0; i < 3; i++)
        {
            logger.LogInformation("Sending test mail {0}...", i);
            await rpcCaller.JsonRemoteCall<SendMailRequestMqttPayload, RpcVoid>(new SendMailRequestMqttPayload
                {
                    Title = "Test Mail",
                    Body = "Test Body",
                    IsAdminReport = true
                }
            );
            await Task.Delay(TimeSpan.FromSeconds(5));
            logger.LogInformation("Mail sent");
        }
    }
}