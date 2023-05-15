using MQTTnet;
using MQTTnet.Client;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Lucky.Home.Services
{
    [DataContract]
    public class RpcVoid
    {
    }

    [DataContract]
    public class RpcErrorResponse
    {
        [DataMember(Name = "error")]
        public string Error { get; set; }
    }

    public class MqttService : ServiceBase
    {
        private readonly MqttFactory mqttFactory;
        private readonly IMqttClient mqttClient;
        private Task connected;

        public MqttService()
        {
            mqttFactory = new MqttFactory();
            mqttClient = mqttFactory.CreateMqttClient();
            connected = Connect();
        }

        private async Task Connect()
        {
            var clientOptions = new MqttClientOptionsBuilder()
              .WithClientId("net")
              .WithTcpServer("localhost")
              .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
              .Build();
            await mqttClient.ConnectAsync(clientOptions);
            Logger.Log("Connected");
        }

        public async Task SubscribeRpc<TReq, TResp>(string topic, Func<TReq, Task<TResp>> handler) where TReq: new() where TResp: new()
        {
            await connected;
            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder().WithTopicFilter(f => f.WithTopic(topic)).Build();
            await mqttClient.SubscribeAsync(mqttSubscribeOptions);
            mqttClient.ApplicationMessageReceivedAsync += async (MqttApplicationMessageReceivedEventArgs args) =>
            {
                if (args.ApplicationMessage.Topic == topic && args.ApplicationMessage.ResponseTopic != null)
                {
                    args.IsHandled = true;
                    byte[] responsePayload = null;
                    try
                    {
                        TReq req = new TReq();
                        if (args.ApplicationMessage.Payload != null)
                        {
                            var deserializer = new DataContractJsonSerializer(typeof(TReq));
                            req = (TReq)deserializer.ReadObject(new MemoryStream(args.ApplicationMessage.Payload));
                        }
                        TResp resp = await handler(req);
                        var responseStream = new MemoryStream();
                        new DataContractJsonSerializer(typeof(TResp)).WriteObject(responseStream, resp);
                        responsePayload = responseStream.ToArray();
                    }
                    catch (Exception exc)
                    {
                        // Send back error as string
                        var responseStream = new MemoryStream();
                        new DataContractJsonSerializer(typeof(RpcErrorResponse)).WriteObject(responseStream, new RpcErrorResponse { Error = exc.Message });
                        responsePayload = responseStream.GetBuffer();
                    }

                    var respMsg = new MqttApplicationMessage();
                    respMsg.Payload = responsePayload;
                    respMsg.CorrelationData = args.ApplicationMessage.CorrelationData;
                    respMsg.Topic = args.ApplicationMessage.ResponseTopic;

                    await mqttClient.PublishAsync(respMsg);
                }
            };
            Logger.Log("Subscribed", "topic", topic);
        }
    }
}
