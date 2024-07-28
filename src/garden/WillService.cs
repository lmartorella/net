using Lucky.Home.Services;

namespace Lucky.Home.Garden;

public class WillService(SerializerFactory serializerFactory) : IMqttWillProvider
{
    public string WillTopic => "ui/garden/state";

    public byte[] WillPayload
    {
        get
        {
            return serializerFactory.Create<StatusType>().Serialize(new StatusType
            {
                OnlineStatus = OnlineStatus.Offline
            });
        }
    }
}
