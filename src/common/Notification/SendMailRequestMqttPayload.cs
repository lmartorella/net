namespace Lucky.Home.Notification;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

/// <summary>
/// Topic ID: notification/send_mail
/// </summary>
[DataContract]
public class SendMailRequestMqttPayload
{
    [NotNull]
    [DataMember(Name = "title")]
    public string Title = null!;

    [NotNull]
    [DataMember(Name = "body")]
    public string Body = null!;

    [DataMember(Name = "isAdminReport")]
    public bool IsAdminReport;
}