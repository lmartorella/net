using Lucky.Garden.Services;

namespace Lucky.Garden;

public class WillService(SerializerFactory serializerFactory) : IMqttWillProvider
{
    public string WillTopic => "garden/status";

    public byte[] WillPayload
    {
        get
        {
            return serializerFactory.Create<StatusType>().Serialize(new StatusType
            {
                StatusCode = StatusCode.Offline,
                Config = new ProgramConfig
                {
                    ProgramCycles = []
                }
            });
        }
    }
}
