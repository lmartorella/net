using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
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
    }

    [DataContract]
    public class RpcErrorResponse : RpcResponse
    {
        [DataMember(Name = "error")]
        public string Error { get; set; }
    }

    public class MqttService : ServiceBase
    {
        private readonly MqttFactory mqttFactory;
        private readonly IMqttClient mqttClient;
        private DataContractJsonSerializer _reqSer;
        private DataContractJsonSerializer _respSer;
        private readonly Dictionary<Type, bool> _reqAdditionalTypes = new Dictionary<Type, bool>();
        private readonly Dictionary<Type, bool> _respAdditionalTypes = new Dictionary<Type, bool>();

        public MqttService()
        {
            mqttFactory = new MqttFactory();
            mqttClient = mqttFactory.CreateMqttClient();
            mqttClient.ConnectAsync(new MqttClientOptions());
        }

        private DataContractJsonSerializer RequestSerializer
        {
            get
            {
                lock (this)
                {
                    return _reqSer ?? (_reqSer = new DataContractJsonSerializer(typeof(RpcRequest), _reqAdditionalTypes.Keys));
                }
            }
        }

        private DataContractJsonSerializer ResponseSerializer
        {
            get
            {
                lock (this)
                {
                    return _respSer ?? (_respSer = new DataContractJsonSerializer(typeof(RpcResponse), _respAdditionalTypes.Keys));
                }
            }
        }

        public void SubscribeRpc<TReq, TResp>(string topic, Func<TReq, Task<TResp>> handler) where TReq: RpcRequest where TResp: RpcResponse
        {
            RegisterAdditionalTypes<TReq, TResp>();
            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder().WithTopicFilter(f => f.WithTopic(topic)).Build();
            mqttClient.SubscribeAsync(mqttSubscribeOptions);
            mqttClient.ApplicationMessageReceivedAsync += async (MqttApplicationMessageReceivedEventArgs args) =>
            {
                if (args.ApplicationMessage.Topic == topic && args.ApplicationMessage.ResponseTopic != null)
                {
                    args.IsHandled = true;
                    byte[] responsePayload = null;
                    try
                    {
                        TReq req = (TReq)RequestSerializer.ReadObject(new MemoryStream(args.ApplicationMessage.Payload));
                        TResp resp = await handler(req);
                        var responseStream = new MemoryStream();
                        ResponseSerializer.WriteObject(responseStream, resp);
                        responsePayload = responseStream.GetBuffer();
                    }
                    catch (Exception exc)
                    {
                        // Send back error as string
                        var responseStream = new MemoryStream();
                        ResponseSerializer.WriteObject(responseStream, new RpcErrorResponse { Error = exc.Message });
                        responsePayload = responseStream.GetBuffer();
                    }

                    var respMsg = new MqttApplicationMessage();
                    respMsg.Payload = responsePayload;
                    respMsg.CorrelationData = args.ApplicationMessage.CorrelationData;
                    respMsg.Topic = args.ApplicationMessage.ResponseTopic;

                    await mqttClient.PublishAsync(respMsg);
                }
            };
        }

        private void RegisterAdditionalTypes<TReq, TResp>() where TReq : RpcRequest where TResp : RpcResponse
        {
            if (!_reqAdditionalTypes.ContainsKey(typeof(TReq)))
            {
                _reqAdditionalTypes[typeof(TReq)] = true;
                _reqSer = null;
            }
            if (!_respAdditionalTypes.ContainsKey(typeof(TReq)))
            {
                _respAdditionalTypes[typeof(TResp)] = true;
                _respSer = null;
            }
        }
    }
}
