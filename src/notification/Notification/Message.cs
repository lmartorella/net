namespace Lucky.Home.Notification;

internal class Message
{
    public string Text { get; set; }

    public object LockObject { get; set; }

    public void Update(string appendText)
    {
        lock (LockObject)
        {
            Text += appendText;
        }
    }
}
