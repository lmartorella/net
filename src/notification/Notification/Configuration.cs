using System.Net;
using Microsoft.Extensions.Configuration;

namespace Lucky.Home.Notification;

/// <summary>
/// Configuration for email SMTP service
/// </summary>
class Configuration(IConfiguration configuration)
{
    public string SmtpHost
    {
        get
        {
            return configuration["smtpHost"] ?? "smtp.host.com";
        }
    }

    public int SmtpPort
    {
        get
        {
            return int.Parse(configuration["smtpPort"] ?? "25");
        }
    }

    public string Sender
    {
        get
        {
            return configuration["sender"] ?? "net@mail.com";
        }
    }
    
    public NetworkCredential Credentials
    {
        get
        {
            var user = configuration["user"] ?? "user";
            var password = configuration["password"] ?? "password";
            return new NetworkCredential(user, password);
        }
    }

    public bool EnableSsl
    {
        get
        {
            return bool.Parse(configuration["enableSsl"] ?? "false");
        }
    }

    public string NotificationRecipient
    {
        get
        {
            return configuration["notificationRecipient"] ?? "user@mail.com";
        }
    }

    public string AdminNotificationRecipient
    {
        get
        {
            return configuration["adminNotificationRecipient"] ?? "admin@mail.com";
        }
    }
}
