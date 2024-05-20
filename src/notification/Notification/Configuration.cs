using System.Net;
using Microsoft.Extensions.Configuration;

namespace Lucky.Home.Notification;

/// <summary>
/// Configuration for email SMTP service
/// </summary>
class Configuration(IConfiguration configuration)
{
    private IConfigurationSection Section
    {
        get
        {
            return configuration.GetSection("notification");
        }
    }

    public bool UseConsole
    {
        get
        {
            return bool.Parse(Section["console"] ?? "false");
        }
    }

    public string SmtpHost
    {
        get
        {
            return Section["smtpHost"] ?? "smtp.host.com";
        }
    }

    public int SmtpPort
    {
        get
        {
            return int.Parse(Section["smtpPort"] ?? "25");
        }
    }

    public string Sender
    {
        get
        {
            return Section["sender"] ?? "net@mail.com";
        }
    }
    
    public NetworkCredential Credentials
    {
        get
        {
            var user = Section["user"] ?? "user";
            var password = Section["password"] ?? "password";
            return new NetworkCredential(user, password);
        }
    }

    public bool EnableSsl
    {
        get
        {
            return bool.Parse(Section["enableSsl"] ?? "false");
        }
    }

    public string NotificationRecipient
    {
        get
        {
            return Section["notificationRecipient"] ?? "user@mail.com";
        }
    }

    public string AdminNotificationRecipient
    {
        get
        {
            return Section["adminNotificationRecipient"] ?? "admin@mail.com";
        }
    }
}
