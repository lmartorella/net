namespace Lucky.Home.Services;

public interface IMqttWillProvider
{
    string WillTopic { get; }
    byte[] WillPayload { get; }
}
