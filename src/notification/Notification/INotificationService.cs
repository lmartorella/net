namespace Lucky.Home.Notification;

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
    /// Enqueue a low-priority notification (sent aggregated on throttle basis).
    /// `messageToAppend` will be appended.
    /// `altMessageToAppendIfStillInQueue` will be used instead of `messageToAppend` if the message is still in queue, if passed.
    /// </summary>
    void EnqueueStatusUpdate(string groupTitle, string messageToAppend, string altMessageToAppendIfStillInQueue = null);
}
