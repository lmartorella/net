using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        private static readonly TimeSpan TimeoutPeriod = TimeSpan.FromSeconds(5);
        private readonly DataContractJsonSerializer errSerializer = new DataContractJsonSerializer(typeof(RpcErrorResponse));

        public MqttService()
        {
            mqttFactory = new MqttFactory();
            mqttClient = mqttFactory.CreateMqttClient();
            connected = Connect();
        }

        private async Task Connect()
        {
            var clientOptions = new MqttClientOptionsBuilder()
              .WithClientId(Assembly.GetEntryAssembly().GetName().Name)
              .WithTcpServer("localhost")
              .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
              .Build();
            await mqttClient.ConnectAsync(clientOptions);
            Logger.Log("Connected");
        }

        public async Task SubscribeJsonValue<T>(string topic, Action<T> handler) where T: class, new() 
        {
            var deserializer = new DataContractJsonSerializer(typeof(T));
            await connected;
            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder().WithTopicFilter(f => f.WithTopic(topic)).Build();
            await mqttClient.SubscribeAsync(mqttSubscribeOptions);
            mqttClient.ApplicationMessageReceivedAsync += async args =>
            {
                if (args.ApplicationMessage.Topic == topic)
                {
                    T req = null;
                    if (args.ApplicationMessage.Payload != null)
                    {
                        req = (T)deserializer.ReadObject(new MemoryStream(args.ApplicationMessage.Payload));
                    }
                    handler(req);
                }
            };
        }

        public async Task JsonPublish<T>(string topic, T value)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            var stream = new MemoryStream();
            serializer.WriteObject(stream, value);

            var message = mqttFactory.CreateApplicationMessageBuilder()
                .WithPayload(stream.GetBuffer())
                .WithTopic(topic).
                Build();
            await mqttClient.PublishAsync(message);
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
                        errSerializer.WriteObject(responseStream, new RpcErrorResponse { Error = exc.Message });
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

        /// <summary>
        /// Subscribe RPC requests in JSON format
        /// </summary>
        public Task SubscribeJsonRpc<TReq, TResp>(string topic, Func<TReq, Task<TResp>> handler) where TReq: class, new() where TResp: class, new()
        {
            var reqSerializer = new DataContractJsonSerializer(typeof(TReq));
            var respDeserializer = new DataContractJsonSerializer(typeof(TResp));
            return SubscribeRawRpcRequest(topic, async payload =>
            {
                TReq req = null;
                if (payload != null)
                {
                    req = (TReq)reqSerializer.ReadObject(new MemoryStream(payload));
                }
                TResp resp = await handler(req);
                var responseStream = new MemoryStream();
                respDeserializer.WriteObject(responseStream, resp);
                return responseStream.ToArray();
            });
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
        /// Enable reception of RPC calls
        /// </summary>
        public async Task RegisterRemoteCalls(string[] topics)
        {
            await connected;
            foreach (string topic in topics)
            {
                await SubscribeRawRpcResponse(topic + "/resp", (handler, payload) =>
                {
                    handler(payload, null);
                });
                await SubscribeRawRpcResponse(topic + "/resp_err", (handler, payload) => {
                    handler(null, new MqttRemoteCallError(Encoding.UTF8.GetString(payload)));
                });
            }
        }

        public async Task<byte[]> RawRemoteCall(string topic, byte[] payload = null)
        {
            await connected;
            var correlationData = Guid.NewGuid();
            var deferred = new TaskCompletionSource<byte[]>();
            requests[correlationData] = new Action<byte[], Exception>((payolad, err) =>
            {
                if (err == null)
                {
                    deferred.TrySetResult(payolad);
                }
                else
                {
                    deferred.TrySetException(err);
                }
            });
            var message = mqttFactory.CreateApplicationMessageBuilder()
                .WithCorrelationData(correlationData.ToByteArray())
                .WithPayload(payload)
                .WithResponseTopic(topic + "/resp")
                .WithTopic(topic).
                Build();
            await mqttClient.PublishAsync(message);
            _ = Task.Delay(TimeoutPeriod).ContinueWith(task =>
            {
                deferred.TrySetCanceled();
            });
            return await deferred.Task;
        }

        public async Task<TResp> JsonRemoteCall<TReq, TResp>(string topic, TReq payload = null) where TReq : class, new() where TResp : class, new()
        {
            var reqSerializer = new DataContractJsonSerializer(typeof(TReq));
            var respDeserializer = new DataContractJsonSerializer(typeof(TResp));
            var requestStream = new MemoryStream();
            reqSerializer.WriteObject(requestStream, payload);

            var responseData = await RawRemoteCall(topic, requestStream.ToArray());
            var response = (TResp)respDeserializer.ReadObject(new MemoryStream(responseData));
            return response;
        }
    }
}
