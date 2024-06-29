using System.Net.Mail;
using Lucky.Home.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lucky.Home.Notification;

class NotificationService(ILogger<NotificationService> logger, Configuration configuration, MqttService mqttService) : BackgroundService
{
    private Dictionary<string, Bucket> _statusBuckets = new Dictionary<string, Bucket>();

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await mqttService.SubscribeJsonRpc<SendMailRequestMqttPayload, RpcVoid>("notification/send_mail", req => 
        {
            SendMail(req.Title, req.Body, req.IsAdminReport);
            return Task.FromResult(new RpcVoid());
        });

        await mqttService.SubscribeJsonRpc<EnqueueStatusUpdateRequestMqttPayload, RpcVoid>("notification/enqueue_status_update", req =>
        {
            EnqueueStatusUpdate(req.GroupTitle, req.MessageToAppend, req.AltMessageToAppendIfStillInQueue);
            return Task.FromResult(new RpcVoid());
        });
    }

    /// <summary>
    /// Send a text mail
    /// </summary>
    public void SendMail(string title, string body, bool isAdminReport)
    {
        logger.LogInformation($"SendingMail, title '{title}'");
        // Don't wait for mail to be sent
        _ = SendMailLoop(title, body, isAdminReport);
    }

    private async Task SendMailLoop(string title, string body, bool isAdminReport)
    {
        // Specify the message content.
        MailMessage message = new MailMessage(configuration.Sender, isAdminReport ? configuration.AdminNotificationRecipient : configuration.NotificationRecipient)
        {
            Subject = title,
            Body = body
        };

        bool sent = false;
        while (!sent)
        {
            sent = await TrySendMail(message);
            if (!sent)
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }
    }

    private async Task<bool> TrySendMail(MailMessage message)
    {
        // Command line argument must the the SMTP host.
        SmtpClient client = new SmtpClient
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Host = configuration.SmtpHost,
            Port = configuration.SmtpPort,
            Credentials = configuration.Credentials,
            EnableSsl = configuration.EnableSsl
        };

        try
        {
            await client.SendMailAsync(message);
            logger.LogInformation($"Mail sent to: {message.To}");
            client.Dispose();
            return true;
        }
        catch (Exception exc)
        {
            try
            {
                client.Dispose();
            }
            catch { }
            logger.LogError(exc, "InSend");
            logger.LogInformation("Retrying in 30 seconds...");
            // Retry
            return false;
        }
    }

    public void EnqueueStatusUpdate(string groupTitle, string messageToAppend, string? altMessageToAppendIfStillInQueue)
    {
        lock (_statusBuckets)
        {
            Bucket? bucket;
            if (!_statusBuckets.TryGetValue(groupTitle, out bucket))
            {
                bucket = new Bucket(this, groupTitle);
                _statusBuckets.Add(groupTitle, bucket);
            }
            bucket!.Enqueue(messageToAppend, altMessageToAppendIfStillInQueue);
        }
    }
}
