namespace Lucky.Home.Notification;

internal class Bucket
{
    private readonly string _groupTitle;
    private readonly NotificationService _svc;
    private List<Message> _messages = new List<Message>();
    private Timer? _timer;

    public Bucket(NotificationService svc, string groupTitle)
    {
        _svc = svc;
        _groupTitle = groupTitle;
    }

    internal void Enqueue(string messageToAppend, string? altMessageToAppendIfStillInQueue = null)
    {
        lock (this)
        {
            if (altMessageToAppendIfStillInQueue != null)
            {
                if (_messages.Count > 0)
                {
                    _messages.Last().Update(altMessageToAppendIfStillInQueue);
                    return;
                }
                messageToAppend = altMessageToAppendIfStillInQueue;
            }
            var message = new Message();
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
                        msg = string.Join(Environment.NewLine, _messages.Select(m => m.Text));
                        _messages.Clear();
                        _timer = null;
                    }
                    if (msg.Trim().Length > 0)
                    {
                        _svc.SendMail(_groupTitle, msg, true);
                    }
                }, null, (int)TimeSpan.FromHours(1).TotalMilliseconds, Timeout.Infinite);
            }
        }
    }
}
