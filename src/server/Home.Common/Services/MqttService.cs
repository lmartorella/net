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
        private readonly Dictionary<Guid, Action<byte[], Exception>> requests = new Dictionary<Guid, Action<byte[], Exception>>();
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

        /// <summary>
        /// Subscribe RPC requests in binary format
        /// </summary>
        public async Task SubscribeRawRpcRequest(string topic, Func<byte[], Task<byte[]>> handler)
        {
            await connected;
            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder().WithTopicFilter(f => f.WithTopic(topic)).Build();
            await mqttClient.SubscribeAsync(mqttSubscribeOptions);
            mqttClient.ApplicationMessageReceivedAsync += async args =>
            {
                if (args.ApplicationMessage.Topic == topic && args.ApplicationMessage.ResponseTopic != null)
                {
                    args.IsHandled = true;
                    byte[] responsePayload = null;
                    try
                    {
                        responsePayload = await handler(args.ApplicationMessage.Payload);
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
            Logger.Log("Subscribed requests", "topic", topic);
        }

        private async Task SubscribeRawRpcResponse(string topic, Action<Action<byte[], Exception>, byte[]> handler)
        {
            await connected;
            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder().WithTopicFilter(f => f.WithTopic(topic)).Build();
            await mqttClient.SubscribeAsync(mqttSubscribeOptions);
            mqttClient.ApplicationMessageReceivedAsync += args =>
            {
                if (args.ApplicationMessage.Topic == topic && args.ApplicationMessage.CorrelationData != null)
                {
                    Action<byte[], Exception> tuple;
                    var correlationData = new Guid(args.ApplicationMessage.CorrelationData);
                    if (requests.TryGetValue(correlationData, out tuple))
                    {
                        requests.Remove(correlationData);
                        handler(tuple, args.ApplicationMessage.Payload);
                        args.IsHandled = true;
                    }
                }
                return Task.CompletedTask;
            };
            Logger.Log("Subscribed responses", "topic", topic);
        }

        /// <summary>
        /// Subscribe RPC requests in JSON format
        /// </summary>
        public Task SubscribeJsonRpc<TReq, TResp>(string topic, Func<TReq, Task<TResp>> handler) where TReq: new() where TResp: new()
        {
            return SubscribeRawRpcRequest(topic, async payload =>
            {
                TReq req = new TReq();
                if (payload != null)
                {
                    var deserializer = new DataContractJsonSerializer(typeof(TReq));
                    req = (TReq)deserializer.ReadObject(new MemoryStream(payload));
                }
                TResp resp = await handler(req);
                var responseStream = new MemoryStream();
                new DataContractJsonSerializer(typeof(TResp)).WriteObject(responseStream, resp);
                return responseStream.ToArray();
            });
        }

        /// <summary>
        /// Enable reception of RPC calls
        /// </summary>
        public async Task RegisterRemoteCalls(string[] topics)
        {
            foreach (string topic in topics)
            {
                await SubscribeRawRpcResponse(topic + "/resp", (handler, payload) =>
                {
                    handler(payload, null);
                });
                await SubscribeRawRpcResponse(topic + "/resp_err", (handler, payload) => {
                    handler(null, new MqttRemoveCallError(Encoding.UTF8.GetString(payload)));
                });
            }
        }

        public async Task<byte[]> RemoteCall(string topic, byte[] payload)
        {
            var correlationData = Guid.NewGuid();
            var deferred = new TaskCompletionSource<byte[]>();
            requests[correlationData] = new Action<byte[], Exception>((payolad, err) =>
            {
                if (err == null)
                {
                    deferred.SetResult(payolad);
                }
                else
                {
                    deferred.SetException(err);
                }
            });
            var message = mqttFactory.CreateApplicationMessageBuilder()
                .WithCorrelationData(correlationData.ToByteArray())
                .WithPayload(payload)
                .WithResponseTopic(topic + "/resp")
                .WithTopic(topic).
                Build();
            await mqttClient.PublishAsync(message);
            return await deferred.Task;
        }
    }
}
