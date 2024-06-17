using System.Net.Mime;

namespace Lucky.Home.Notification;

/// <summary>
/// A status message that will be aggregated in a single message
/// </summary>
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
    /// <returns>True if the handler was successful called, false otherwise (update sent)</returns>
    bool Update(Action updateHandler);
}

/// <summary>
/// Service for notifications (e.g. emails...)
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send an immediate text mail (not coalesced)
    /// </summary>
    Task SendMail(string title, string body, bool isAdminReport);

    /// <summary>
    /// Send an immediate HTML mail (not coalesced)
    /// </summary>
    Task SendHtmlMail(string title, string htmlBody, bool isAdminReport, IEnumerable<Tuple<Stream, ContentType, string>> attachments = null);

    /// <summary>
    /// Enqueue a low-priority notification (sent aggregated on throttle basis).
    /// Returns an object to modify the status update, if the update was not yet send.
    /// </summary>
    IStatusUpdate EnqueueStatusUpdate(string groupTitle, string message);
}
