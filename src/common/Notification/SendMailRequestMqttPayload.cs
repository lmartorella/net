namespace Lucky.Home.Notification;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

[DataContract]
public class SendMailRequestMqttPayload
{
    [NotNull]
    [DataMember(Name = "name")]
    public string Title = null!;

    [NotNull]
    [DataMember(Name = "body")]
    public string Body = null!;

    [DataMember(Name = "isAdminReport")]
    public bool IsAdminReport;
}