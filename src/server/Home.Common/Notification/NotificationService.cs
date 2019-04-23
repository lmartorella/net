using System;
using Lucky.Services;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Net.Mime;
using System.IO;
using System.Runtime.Serialization;

namespace Lucky.Home.Notification
{
    public interface IStatusUpdate
    {
        /// <summary>
        /// Get/set the timestamp (during <see cref="Update"/>)
        /// </summary>
        DateTime TimeStamp { get; set; }

        /// <summary>
        /// Get/set the message (during <see cref="Update"/>)
        /// </summary>
        string Text { get; set; }

        /// <summary>
        /// Update a message. 
        /// </summary>
        /// <param name="updateHandler">The handler will be called if the message can be updated</param>
        /// <returns>True if the handler was successfull called, false otherwise (update sent)</returns>
        bool Update(Action updateHandler);
    }

    public interface INotificationService : IService
    {
        /// <summary>
        /// Send an immediate text mail (not coalesced)
        /// </summary>
        Task SendMail(string title, string body);

        /// <summary>
        /// Send an immediate HTML mail (not coalesced)
        /// </summary>
        Task SendHtmlMail(string title, string htmlBody, IEnumerable<Tuple<Stream, ContentType, string>> attachments = null);

        /// <summary>
        /// Enqueue a low-priority notification (sent aggregated each hour).
        /// Returns an object to modify the status update, if the update was not yet send.
        /// </summary>
        IStatusUpdate EnqueueStatusUpdate(string groupTitle, string message);
    }

    [DataContract]
    public class MailConfiguration 
    {
        [DataMember]
        public string SmtpHost { get; set; }

        [DataMember]
        public int SmtpPort { get; set; }

        [DataMember]
        public bool EnableSsl { get; set; }

        [DataMember]
        public string To { get; set; }

        [DataMember]
        public string Sender { get; set; }

        [DataMember]
        public string User { get; set; }

        [DataMember]
        public string Password { get; set; }
    }

    class NotificationService : ServiceBaseWithData<MailConfiguration>, INotificationService
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
                        _timer = new Timer(HandleTimer, null, (int)TimeSpan.FromHours(1).TotalMilliseconds, Timeout.Infinite);
                    }
                }
            }

            private void HandleTimer(object state)
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
                    _ = _svc.SendMail(_groupTitle, msg);
                }
            }
        }

        private Dictionary<string, Bucket> _statusBuckets = new Dictionary<string, Bucket>();

        /// <summary>
        /// Send a text mail
        /// </summary>
        public async Task SendMail(string title, string body)
        {
            var configuration = State;

            Logger.Log("SendingMail", "title", title, "body", body);

            // Specify the message content.
            MailMessage message = new MailMessage(configuration.Sender, configuration.To);
            message.Subject = title;
            message.Body = body;

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

        /// <summary>
        /// Send a HTML mail
        /// </summary>
        public async Task SendHtmlMail(string title, string htmlBody, IEnumerable<Tuple<Stream, ContentType, string>> attachments = null)
        {
            var configuration = State;

            if (attachments == null)
            {
                attachments = new Tuple<Stream, ContentType, string>[0];
            }
            Logger.Log("SendingHtmlMail", "title", title, "attch", attachments.Count());

            // Specify the message content.
            MailMessage message = new MailMessage(configuration.Sender, configuration.To);
            message.Subject = title;

            var alternateView = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);

            foreach (var attInfo in attachments)
            {
                LinkedResource resource = new LinkedResource(attInfo.Item1, attInfo.Item2);
                resource.ContentId = attInfo.Item3;
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
            var configuration = State;

            // Command line argument must the the SMTP host.
            SmtpClient client = new SmtpClient();
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Host = configuration.SmtpHost;
            client.Port = configuration.SmtpPort;
            client.Credentials = new NetworkCredential(configuration.User, configuration.Password);
            client.EnableSsl = configuration.EnableSsl;

            try
            {
                await client.SendMailAsync(message);
                Logger.Log("Mail sent to: " + configuration.To);
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
                Logger.Exception(exc, false);
                Logger.Log("Retrying in 30 seconds...");
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
                if (!_statusBuckets.TryGetValue(groupTitle, out bucket))
                {
                    bucket = new Bucket(this, groupTitle);
                    _statusBuckets.Add(groupTitle, bucket);
                }
                bucket.Enqueue(message);
            }
        }
    }
}
