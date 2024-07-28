using Lucky.Home.Notification;
using Microsoft.Extensions.Hosting;

namespace Lucky.Home.Services;

public class NotificationService(MqttService mqttService) : BackgroundService
{
    private MqttService.RpcOriginator sendMailRpcOriginator;
    private MqttService.RpcOriginator statusUpdateRpcOriginator;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        sendMailRpcOriginator = await mqttService.RegisterRpcOriginator("notification/send_mail", TimeSpan.FromSeconds(10));
        statusUpdateRpcOriginator = await mqttService.RegisterRpcOriginator("notification/enqueue_status_update", TimeSpan.FromSeconds(10));
    }

    public async Task EnqueueStatusUpdate(string groupTitle, string messageToAppend, string altMessageToAppendIfStillInQueue = null)
    {
        await statusUpdateRpcOriginator.JsonRemoteCall<EnqueueStatusUpdateRequestMqttPayload, RpcVoid>(new EnqueueStatusUpdateRequestMqttPayload() 
        {
            GroupTitle = groupTitle,
            MessageToAppend = messageToAppend,
            AltMessageToAppendIfStillInQueue = altMessageToAppendIfStillInQueue
        });
    }

    public async Task SendMail(string title, string body, bool isAdminReport)
    {
        await sendMailRpcOriginator.JsonRemoteCall<SendMailRequestMqttPayload, RpcVoid>(new SendMailRequestMqttPayload() 
        {
            Body = body,
            Title = title,
            IsAdminReport = isAdminReport
        });
    }
}
