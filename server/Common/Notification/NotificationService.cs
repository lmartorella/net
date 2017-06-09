using System;
using Lucky.Services;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;

namespace Lucky.Home.Notification
{
    public interface INotificationService : IService
    {
        /// <summary>
        /// Send a mail
        /// </summary>
        Task SendMail(string title, string body);
    }

    class NotificationService : ServiceBase, INotificationService
    {
        private string _smtpHost = "=HIDDEN=";
        private int _smtpPort = 587;
        private bool _enableSsl = true;
        private string _dest = "=HIDDEN=";
        private string _sender = "=HIDDEN=";
        private string _user = "=HIDDEN=";
        private string _password = "=HIDDEN=";

        /// <summary>
        /// Send a mail
        /// </summary>
        public async Task SendMail(string title, string body)
        {
            Logger.Log("SendingMail", "title", title, "body", body);

            // Command line argument must the the SMTP host.
            SmtpClient client = new SmtpClient();
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Host = _smtpHost;
            client.Port = _smtpPort;
            client.Credentials = new NetworkCredential(_user, _password);
            client.EnableSsl = _enableSsl;

            // Specify the message content.
            MailMessage message = new MailMessage(_sender, _dest);
            message.Subject = title;
            message.Body = body;

            try
            {
                await client.SendMailAsync(message);
                Logger.Log("Mail sent to: " + _dest);
                client.Dispose();
            }
            catch (Exception exc)
            {
                try
                {
                    client.Dispose();
                }
                catch { }
                Logger.Exception(exc);
                Logger.Log("Retrying in 30 seconds...");
                // Retry
                await Task.Delay(TimeSpan.FromSeconds(30)).ContinueWith(t => SendMail(title, body));
            }
        }
    }
}
