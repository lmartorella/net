namespace Lucky.Garden.Services
{
    public class MqttRemoteCallError : Exception
    {
        public MqttRemoteCallError(string message)
            :base(message)
        {
        }
    }
}
