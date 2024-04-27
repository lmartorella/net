namespace Lucky.Garden.Services;

public interface IMqttWillProvider
{
    string WillTopic { get; }
    byte[] WillPayload { get; }
}
