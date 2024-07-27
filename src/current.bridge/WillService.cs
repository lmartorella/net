using System.Text;
using Lucky.Home.Services;
using Lucky.Home.Solar;

namespace Lucky.Home;

public class WillService : IMqttWillProvider
{
    public string WillTopic => Constants.CurrentSensorStateTopicId;

    public byte[] WillPayload
    {
        get
        {
            return Encoding.UTF8.GetBytes(DeviceState.Offline.ToString());
        }
    }
}
