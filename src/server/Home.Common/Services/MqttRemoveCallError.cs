using System;

namespace Lucky.Home.Services
{
    public class MqttRemoveCallError : Exception
    {
        public MqttRemoveCallError(string message)
            :base(message)
        {
        }
    }
}
