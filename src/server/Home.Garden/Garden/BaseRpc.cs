using Lucky.Home.Services;

namespace Lucky.Home.Devices.Garden
{
    class BaseRpc
    {
        protected MqttService mqttService;
        
        public BaseRpc()
        {
            mqttService = Manager.GetService<MqttService>();
        }

        public bool IsOnline = false;
    }
}
