using MQTTnet;
using MQTTnet.Client;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Lucky.Home.Services
{
    [DataContract]
    public class RpcRequest
    {
    }

    [DataContract]
    public class RpcResponse
    {
        [DataMember(Name = "error")]
        public string Error { get; set; }
    }

    public class MqttService : ServiceBase
    {
        private readonly MqttFactory mqttFactory;
        private readonly IMqttClient mqttClient;

        public MqttService()
        {
            mqttFactory = new MqttFactory();
            mqttClient = mqttFactory.CreateMqttClient();
            mqttClient.ConnectAsync(new MqttClientOptions());
        }

        public void SubscribeRpc<TReq, TResp>(string topic, Func<TReq, Task<TResp>> handler)
        {
            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder().WithTopicFilter(f => f.WithTopic(topic)).Build();
            mqttClient.SubscribeAsync(mqttSubscribeOptions);
            mqttClient.ApplicationMessageReceivedAsync += async (MqttApplicationMessageReceivedEventArgs args) =>
            {
                if (args.ApplicationMessage.Topic == topic && args.ApplicationMessage.ResponseTopic != null)
                {
                    var result = await handler(args);
                    args.IsHandled = true;
                    await mqttClient.PublishAsync(result);
                }
            };
        }
    }
}
