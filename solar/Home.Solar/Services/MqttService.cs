using Lucky.Home.Solar;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Lucky.Home.Services;

/// <summary>
/// Used for empty JSON messages types
/// </summary>
[DataContract]
public class RpcVoid
{
}

/// <summary>
/// Implements plain topic subscribe/publish and RPCs.
/// Errors are sent using the content-type "application/net_err+text"
/// Both typed JSON messages and raw supports null/empty payload to transfer nulls/void.
/// </summary>
public class MqttService : ServiceBase
{
    private readonly SerializerFactory serializerFactory = new SerializerFactory();

    private readonly MqttFactory mqttFactory;
    private readonly IManagedMqttClient mqttClient;
    private const string ErrContentType = "application/net_err+text";

    private sealed class ExceptionForwarderLogger : IMqttNetLogger
    {
        private readonly ILogger logger;

        public ExceptionForwarderLogger(ILogger logger) 
        {
            this.logger = logger;
        }

        public bool IsEnabled { get; set; } = true;

        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
        {
            if (logLevel == MqttNetLogLevel.Error && exception != null)
            {
                logger.Exception(exception);
                Environment.Exit(1);
            }
        }
    }

    public MqttService()
    {
        mqttFactory = new MqttFactory();
        mqttClient = mqttFactory.CreateManagedMqttClient(new ExceptionForwarderLogger(Logger));
        _ = Connect();
        mqttClient.ConnectedAsync += (e) =>
        {
            Logger.Log("Connected");
            return Task.CompletedTask;
        };
        mqttClient.DisconnectedAsync += (e) =>
        {
            Logger.Log("Disconnected, reconnecting", "reason", e.ReasonString);
            return Task.CompletedTask;
        };
        mqttClient.ConnectingFailedAsync += (e) =>
        {
            Logger.Log("Connecting Failed");
            return Task.CompletedTask;
        };
        mqttClient.ConnectionStateChangedAsync += (e) =>
        {
            Logger.Log(string.Format("ConnectionStateChangedAsync: {0}", mqttClient.IsConnected));
            return Task.CompletedTask;
        };
        mqttClient.SynchronizingSubscriptionsFailedAsync += (e) => 
        {
            Logger.Log(string.Format("SynchronizingSubscriptionsFailedAsync: {0}. Added: {1}, Removed {2}", e.Exception?.Message, e.AddedSubscriptions?.Count, e.RemovedSubscriptions?.Count));
            Logger.Exception(e.Exception, "SynchronizingSubscriptionsFailedAsync");
            Environment.Exit(1);
            return Task.CompletedTask;
        };
    }

    private async Task Connect()
    {
        var clientOptionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(Assembly.GetEntryAssembly().GetName().Name)
                .WithTcpServer("127.0.0.1")
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
                .WithCleanSession(true)
                .WithCleanStart(true);

        clientOptionsBuilder
            .WithWillTopic(UserInterface.Topic)
            .WithWillPayload(UserInterface.WillPayload);

        var managedClientOptions = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(3.5))
            .WithClientOptions(clientOptionsBuilder.Build()).
            Build();
        await mqttClient.StartAsync(managedClientOptions);
        Logger.Log("Started");
    }

    private async Task SubscribeTopic(string topic)
    {
        if (mqttClient.IsConnected)
        {
            Logger.LogInfoFormat("Subscribing topic {0}", topic);
            var mqttSubscribeOptions = new MqttTopicFilterBuilder().WithTopic(topic).Build();
            await mqttClient.SubscribeAsync(new[] { mqttSubscribeOptions });
        }
        else
        {
            Logger.LogInfoFormat("Enqueued subscription to topic {0}", topic);
        }
    }

    /// <summary>
    /// Subscribe a raw binary MQTT topic 
    /// Doesn't support errors
    /// </summary>
    public async Task SubscribeRawTopic(string topic, Func<byte[], Task> handler)
    {
        await SubscribeTopic(topic);
        mqttClient.ApplicationMessageReceivedAsync += async args =>
        {
            var msg = args.ApplicationMessage;
            if (msg.Topic == topic)
            {
                if (msg.PayloadSegment.Array != null)
                {
                    await handler(msg.PayloadSegment.Array);
                }
                else 
                {
                    await handler(new byte[0]);
                }
            }
        };
    }

    /// <summary>
    /// Subscribe a MQTT topic that talks JSON. 
    /// Doesn't support errors
    /// </summary>
    public Task SubscribeJsonTopic<T>(string topic, Func<T, Task> handler) where T: class, new() 
    {
        var deserializer = new DataContractJsonSerializer(typeof(T));
        return SubscribeRawTopic(topic, async msg =>
        {
            T req = null;
            if (msg.Length> 0)
            {
                req = (T)deserializer.ReadObject(new MemoryStream(msg));
            }
            await handler(req);
        });
    }

    /// <summary>
    /// Send raw MQTT binary topic. 
    /// Doesn't support errors
    /// </summary>
    public async Task RawPublish(string topic, byte[] value)
    {
        if (!mqttClient.IsConnected)
        {
            throw new MqttRemoteCallError("Broker not connected");
        }

        var message = mqttFactory.CreateApplicationMessageBuilder()
            .WithPayload(value)
            .WithTopic(topic).
            Build();
        await mqttClient.InternalClient.PublishAsync(message);
    }

    /// <summary>
    /// Send MQTT topic that talks JSON. 
    /// Doesn't support errors
    /// </summary>
    public Task JsonPublish<T>(string topic, T value)
    {
        var serializer = new DataContractJsonSerializer(typeof(T));
        var stream = new MemoryStream();
        serializer.WriteObject(stream, value);
        return RawPublish(topic, stream.ToArray());
    }

    /// <summary>
    /// Enable reception of RPC response originated by the called party
    /// </summary>
    public Task<RpcOriginator> RegisterRpcOriginator(string topic)
    {
        return RegisterRpcOriginator(topic, TimeSpan.Zero);
    }

    /// <summary>
    /// Enable reception of RPC response originated by the called party
    /// </summary>
    public async Task<RpcOriginator> RegisterRpcOriginator(string topic, TimeSpan timeout)
    {
        var originator = new RpcOriginator(this, topic, timeout);
        await originator.SubscribeRawRpcResponse();
        return originator;
    }

    public class RpcOriginator
    {
        private readonly Dictionary<Guid, TaskCompletionSource<byte[]>> requests = new Dictionary<Guid, TaskCompletionSource<byte[]>>();
        private readonly MqttService owner;
        private readonly string topic;
        private readonly SerializerFactory serializerFactory;
        private readonly TimeSpan timeout;

        public RpcOriginator(MqttService owner, string topic, TimeSpan timeout)
        {
            this.owner = owner;
            this.serializerFactory = owner.serializerFactory;
            this.topic = topic;
            this.timeout = timeout;
        }

        public async Task SubscribeRawRpcResponse()
        {
            var responseTopic = topic + "/resp";
            await owner.SubscribeTopic(responseTopic);
            owner.mqttClient.ApplicationMessageReceivedAsync += args =>
            {
                var msg = args.ApplicationMessage;
                if (msg.Topic == responseTopic && msg.CorrelationData != null)
                {
                    TaskCompletionSource<byte[]?> request;
                    var correlationData = new Guid(msg.CorrelationData);
                    if (requests.TryGetValue(correlationData, out request!))
                    {
                        requests.Remove(correlationData);
                        args.IsHandled = true;

                        if (msg.ContentType == ErrContentType)
                        {
                            request.TrySetException(new MqttRemoteCallError(Encoding.UTF8.GetString(msg.PayloadSegment.Array!)));
                        }
                        else
                        {
                            request.TrySetResult(msg.PayloadSegment.Count > 0 ? msg.PayloadSegment.Array! : null);
                        }
                    }
                }
                return Task.CompletedTask;
            };
            owner.Logger.LogInfoFormat("Subscribed responses, topic {0}", responseTopic);
        }

        public async Task<byte[]> RawRemoteCall(byte[]? payload = null)
        {
            if (!owner.mqttClient.IsConnected)
            {
                throw new MqttRemoteCallError("Broker not connected");
            }

            var correlationData = Guid.NewGuid();
            var request = new TaskCompletionSource<byte[]>();
            requests[correlationData] = request;
            var builder = owner.mqttFactory.CreateApplicationMessageBuilder()
                .WithCorrelationData(correlationData.ToByteArray());
            if (payload != null)
            {
                builder.WithPayload(payload);
            }
            var message = builder.WithResponseTopic(topic + "/resp")
                .WithTopic(topic).
                Build();
            await owner.mqttClient.InternalClient.PublishAsync(message);
            // Send: wait for response
            if (timeout > TimeSpan.Zero) {
                _ = Task.Delay(timeout).ContinueWith(task =>
                {
                    requests.Remove(correlationData);
                    request.TrySetCanceled();
                });
            }
            return await request.Task;
        }

        public async Task<TResp?> JsonRemoteCall<TReq, TResp>(TReq? payload = null) where TReq : class, new() where TResp : class, new()
        {
            var reqSerializer = serializerFactory.Create<TReq>();
            var respDeserializer = serializerFactory.Create<TResp>();
            var responseData = await RawRemoteCall(reqSerializer.Serialize(payload));
            return respDeserializer.Deserialize(responseData);
        }
    }

    private async Task ProcessRpc(MqttApplicationMessage msg, Func<byte[]?, Task<byte[]>> handler)
    {
        byte[] responsePayload;
        string? contentType = null;
        try
        {
            responsePayload = await handler(msg.PayloadSegment.Array);
        }
        catch (Exception exc)
        {
            // Send back error as string
            Logger.Exception(exc, "Handling RPC");
            responsePayload = Encoding.UTF8.GetBytes($"{exc.GetType().Name}: {exc.Message}");
            contentType = ErrContentType;
        }

        var respMsg = new MqttApplicationMessage()
        {
            PayloadSegment = new ArraySegment<byte>(responsePayload ?? new byte[0]),
            ContentType = contentType,
            CorrelationData = msg.CorrelationData,
            Topic = msg.ResponseTopic
        };

        await mqttClient.InternalClient.PublishAsync(respMsg);
    }

    /// <summary>
    /// Subscribe RPC requests in binary format
    /// </summary>
    public async Task SubscribeRawRpc(string topic, Func<byte[]?, Task<byte[]>> handler)
    {
        await SubscribeTopic(topic);
        mqttClient.ApplicationMessageReceivedAsync += args =>
        {
            var msg = args.ApplicationMessage;
            if (msg.Topic == topic && msg.ResponseTopic != null)
            {
                args.IsHandled = true;
                _ = ProcessRpc(msg, handler);
            }
            return Task.CompletedTask;
        };
        Logger.LogInfoFormat("Created RPC endpoint, topic {0}", topic);
    }

    /// <summary>
    /// Subscribe RPC requests in JSON format
    /// </summary>
    public Task SubscribeJsonRpc<TReq, TResp>(string topic, Func<TReq, Task<TResp>> handler) where TReq: class, new() where TResp: class, new()
    {
        var reqSerializer = serializerFactory.Create<TReq>();
        var respDeserializer = serializerFactory.Create<TResp>();
        return SubscribeRawRpc(topic, async payload =>
        {
            var msg = reqSerializer.Deserialize(payload);
            TResp? resp = default;
            if (msg != null)
            {
                resp = await handler(msg);
            }
            return respDeserializer.Serialize(resp);
        });
    }
}
