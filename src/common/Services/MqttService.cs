using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using System.Runtime.Serialization;
using System.Text;

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
public class MqttService
{
    private readonly ILogger<MqttService> logger;
    private readonly IHostEnvironment hostEnvironment;
    private readonly IMqttWillProvider? mqttWillProvider;
    private readonly SerializerFactory serializerFactory;
    private readonly MqttFactory mqttFactory;
    private readonly IManagedMqttClient mqttClient;
    private const string ErrContentType = "application/net_err+text";

    private sealed class ExceptionForwarderLogger(ILogger<MqttService> logger) : IMqttNetLogger
    {
        public bool IsEnabled { get; set; } = true;

        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
        {
            if (logLevel == MqttNetLogLevel.Error && exception != null)
            {
                logger.LogError(exception, "MqttPublish");
                Environment.Exit(1);
            }
        }
    }

    public MqttService(ILogger<MqttService> logger, IHostEnvironment hostEnvironment, SerializerFactory serializerFactory, IMqttWillProvider mqttWillProvider = null)
    {
        this.logger = logger;
        this.hostEnvironment = hostEnvironment;
        this.mqttWillProvider = mqttWillProvider;
        this.serializerFactory = serializerFactory;
        
        mqttFactory = new MqttFactory();
        mqttClient = mqttFactory.CreateManagedMqttClient(new ExceptionForwarderLogger(logger));
        _ = Connect();
        mqttClient.ConnectedAsync += (e) =>
        {
            logger.LogInformation("Connected");
            return Task.CompletedTask;
        };
        mqttClient.DisconnectedAsync += (e) =>
        {
            logger.LogInformation("Disconnected, reconnecting: {0}", e.ReasonString);
            return Task.CompletedTask;
        };
    }

    private async Task Connect()
    {
        var clientOptionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(hostEnvironment.ApplicationName)
                .WithTcpServer("127.0.0.1")
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500);
        if (mqttWillProvider != null) 
        {
            clientOptionsBuilder
                .WithWillTopic(mqttWillProvider.WillTopic)
                .WithWillPayload(mqttWillProvider.WillPayload);
        }
        var managedClientOptions = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(3.5))
            .WithMaxPendingMessages(1)
            .WithPendingMessagesOverflowStrategy(MQTTnet.Server.MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
            .WithClientOptions(clientOptionsBuilder.Build()).
            Build();
        await mqttClient.StartAsync(managedClientOptions);
        logger.LogInformation("Started");
    }

    /// <summary>
    /// Subscribe a raw binary MQTT topic 
    /// Doesn't support errors
    /// </summary>
    public async Task SubscribeRawTopic(string topic, Func<byte[], Task> handler)
    {
        var mqttSubscribeOptions = new MqttTopicFilterBuilder().WithTopic(topic).Build();
        await mqttClient.SubscribeAsync([mqttSubscribeOptions]);
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
                    await handler([]);
                }
            }
        };
    }

    /// <summary>
    /// Subscribe a MQTT topic that talks JSON. 
    /// Doesn't support errors
    /// </summary>
    public Task SubscribeJsonTopic<T>(string topic, Func<T?, Task> handler) where T: class, new() 
    {
        var deserializer = serializerFactory.Create<T>();
        return SubscribeRawTopic(topic, msg => handler(deserializer.Deserialize(msg)));
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
        var serializer = serializerFactory.Create<T>();
        return RawPublish(topic, serializer.Serialize(value));
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
        private readonly SerializerFactory serializerFactory;
        private readonly string topic;
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
            var mqttSubscribeOptions = new MqttTopicFilterBuilder().WithTopic(responseTopic).Build();
            await owner.mqttClient.SubscribeAsync(new[] { mqttSubscribeOptions });
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
            owner.logger.LogInformation("Subscribed responses, topic {0}", responseTopic);
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
            logger.LogError(exc, "Handling RPC");
            responsePayload = Encoding.UTF8.GetBytes($"{exc.GetType().Name}: {exc.Message}");
            contentType = ErrContentType;
        }

        var respMsg = new MqttApplicationMessage()
        {
            PayloadSegment = new ArraySegment<byte>(responsePayload ?? []),
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
        var mqttSubscribeOptions = new MqttTopicFilterBuilder().WithTopic(topic).Build();
        await mqttClient.SubscribeAsync(new[] { mqttSubscribeOptions });
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
        logger.LogInformation("Subscribed requests, topic {0}", topic);
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
