namespace Lucky.Home.Notification;

internal class Message : IStatusUpdate
{
    private bool _sent;

    public DateTime TimeStamp { get; set; }

    public string Text { get; set; }

    public object LockObject { get; set; }

    public string Send()
    {
        lock(this)
        {
            _sent = true;
            return ToString();
        }
    }

    public bool Update(Action updateHandler)
    {
        lock (LockObject)
        {
            if (_sent)
            {
                return false;
            }
            else
            {
                updateHandler();
                return true;
            }
        }
    }

    public override string ToString()
    {
        return TimeStamp.ToString("HH:mm:ss") + ": " + Text;
    }
}
