using System.Net.Mail;
using System.Net.Mime;
using Microsoft.Extensions.Logging;

namespace Lucky.Home.Notification;

class NotificationService(ILogger<NotificationService> logger, Configuration configuration) : INotificationService
{
    private class Message : IStatusUpdate
    {
        private bool _sent;

        public DateTime TimeStamp { get; set; }

        public string Text { get; set; }

        public object LockObject { get; set; }

        public string Send()
        {
            lock(this)
            {
                _sent = true;
                return ToString();
            }
        }

        public bool Update(Action updateHandler)
        {
            lock (LockObject)
            {
                if (_sent)
                {
                    return false;
                }
                else
                {
                    updateHandler();
                    return true;
                }
            }
        }

        public override string ToString()
        {
            return TimeStamp.ToString("HH:mm:ss") + ": " + Text;
        }
    }

    private class Bucket
    {
        private readonly string _groupTitle;
        private readonly NotificationService _svc;
        private List<Message> _messages = new List<Message>();
        private Timer _timer;

        public Bucket(NotificationService svc, string groupTitle)
        {
            _svc = svc;
            _groupTitle = groupTitle;
        }

        internal void Enqueue(Message message)
        {
            lock (this)
            {
                message.LockObject = this;
                _messages.Add(message);
                // Start timer
                if (_timer == null)
                {
                    _timer = new Timer(_ =>
                    {
                        string msg;
                        lock (this)
                        {
                            // Send a single mail with all the content
                            msg = string.Join(Environment.NewLine, _messages.Select(m => m.Send()));
                            _messages.Clear();
                            _timer = null;
                        }
                        if (msg.Trim().Length > 0)
                        {
                            _ = _svc.SendMail(_groupTitle, msg, true);
                        }
                    }, null, (int)TimeSpan.FromHours(1).TotalMilliseconds, Timeout.Infinite);
                }
            }
        }
    }

    private Dictionary<string, Bucket> _statusBuckets = new Dictionary<string, Bucket>();

    /// <summary>
    /// Send a text mail
    /// </summary>
    public async Task SendMail(string title, string body, bool isAdminReport)
    {
        logger.LogInformation($"SendingMail, title '{title}'");

        if (configuration.UseConsole)
        {
            Console.WriteLine($"Mock of mail to be send: '{title}'");
            Console.WriteLine();
            Console.WriteLine(body);
            Console.WriteLine();
        }
        else
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
    }

    /// <summary>
    /// Send a HTML mail
    /// </summary>
    public async Task SendHtmlMail(string title, string htmlBody, bool isAdminReport, IEnumerable<Tuple<Stream, ContentType, string>> attachments = null)
    {
        if (attachments == null)
        {
            attachments = Array.Empty<Tuple<Stream, ContentType, string>>();
        }
        logger.LogInformation($"SendingHtmlMail, title '{title}', attch {attachments.Count()}");

        // Specify the message content.
        MailMessage message = new MailMessage(configuration.Sender, isAdminReport ? configuration.AdminNotificationRecipient : configuration.NotificationRecipient)
        {
            Subject = title
        };

        var alternateView = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);

        foreach (var attInfo in attachments)
        {
            LinkedResource resource = new LinkedResource(attInfo.Item1, attInfo.Item2)
            {
                ContentId = attInfo.Item3
            };
            alternateView.LinkedResources.Add(resource);
        }

        message.AlternateViews.Add(alternateView);
        message.IsBodyHtml = true;

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

    public IStatusUpdate EnqueueStatusUpdate(string groupTitle, string text)
    {
        var message = new Message { TimeStamp = DateTime.Now, Text = text };
        EnqueueInStatusBucket(groupTitle, message);
        return message;
    }

    private void EnqueueInStatusBucket(string groupTitle, Message message)
    {
        lock (_statusBuckets)
        {
            Bucket bucket;
            if (!_statusBuckets.TryGetValue(groupTitle, out bucket!))
            {
                bucket = new Bucket(this, groupTitle);
                _statusBuckets.Add(groupTitle, bucket);
            }
            bucket.Enqueue(message);
        }
    }
}
