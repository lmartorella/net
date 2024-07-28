using System.Text;
using Lucky.Home.Services;
using Lucky.Home.Solar;

namespace Lucky.Home;

public class WillService(SerializerFactory serializerFactory) : IMqttWillProvider
{
    public string WillTopic => Constants.SolarStateTopicId;

    public byte[] WillPayload
    {
        get
        {
            return Encoding.UTF8.GetBytes(DeviceState.Offline.ToString());
        }
    }
}
