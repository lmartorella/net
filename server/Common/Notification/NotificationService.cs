using System;
using Lucky.Services;
using System.Net.Mail;
using System.Net;

namespace Lucky.Home.Notification
{
    public interface INotificationService : IService
    {
        /// <summary>
        /// Send a mail
        /// </summary>
        void SendMail(string title, string body);
    }

    class NotificationService : ServiceBase, INotificationService
    {
        private string _smtpHost = "=HIDDEN=";
        private int _smtpPort = 587;
        private bool _enableSsl = true;
        private string _dest = "=HIDDEN=";
        private string _sender = "=HIDDEN=";

        /// <summary>
        /// Send a mail
        /// </summary>
        public void SendMail(string title, string body)
        {
            Logger.Log("SendingMail", "title", title);

            // Command line argument must the the SMTP host.
            SmtpClient client = new SmtpClient();
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            //client.UseDefaultCredentials = false;
            client.Host = _smtpHost;
            client.Port = _smtpPort;
            client.Credentials = new NetworkCredential("=HIDDEN=", "=HIDDEN=");
            client.EnableSsl = _enableSsl;

            // Specify the message content.
            MailMessage message = new MailMessage(_sender, _dest);
            message.Subject = title;
            message.Body = body;

            // Set the method that is called back when the send operation ends.
            client.SendCompleted += (o, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Exception(e.Error);
                }
                else if (e.Cancelled)
                {
                    Logger.Log("MailSendCancelled");
                }
                else
                {
                    Logger.Log("Mail sent to: " + _dest);
                }
            };
            client.SendAsync(message, null);
        }
    }
}
