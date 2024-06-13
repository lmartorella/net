namespace Lucky.Home.Notification;

internal class Bucket
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
                _timer = new Timer(_ =>
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
                        _ = _svc.SendMail(_groupTitle, msg, true);
                    }
                }, null, (int)TimeSpan.FromHours(1).TotalMilliseconds, Timeout.Infinite);
            }
        }
    }
}
