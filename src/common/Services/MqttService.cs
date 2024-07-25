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

    public MqttService(ILogger<MqttService> logger, IHostEnvironment hostEnvironment, SerializerFactory serializerFactory, IMqttWillProvider mqttWillProvider = null)
    {
        this.logger = logger;
        this.hostEnvironment = hostEnvironment;
        this.mqttWillProvider = mqttWillProvider;
        this.serializerFactory = serializerFactory;
        
        var mqttLogger = new MqttNetEventLogger();
        mqttLogger.LogMessagePublished += (_, e) => OnMessagePublished(e.LogMessage);

        mqttFactory = new MqttFactory(mqttLogger);
        mqttClient = mqttFactory.CreateManagedMqttClient();
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
        mqttClient.ConnectingFailedAsync += (e) =>
        {
            logger.LogInformation("Connecting Failed");
            return Task.CompletedTask;
        };
        mqttClient.ConnectionStateChangedAsync += (e) =>
        {
            logger.LogInformation("ConnectionStateChangedAsync: {0}", mqttClient.IsConnected);
            return Task.CompletedTask;
        };
        mqttClient.SynchronizingSubscriptionsFailedAsync += (e) => 
        {
            logger.LogInformation("SynchronizingSubscriptionsFailedAsync: {0}. Added: {1}, Removed {2}", e.Exception?.Message, e.AddedSubscriptions?.Count, e.RemovedSubscriptions?.Count);
            logger.LogCritical(e.Exception, "SynchronizingSubscriptionsFailedAsync");
            Environment.Exit(1);
            return Task.CompletedTask;
        };
    }

    private void ReportException(Exception exc)
    {
        logger.LogError(exc, "Unhandled Exception");
    }

    private void OnMessagePublished(MqttNetLogMessage message)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(message.ToString());
        }
        if (message.Level == MqttNetLogLevel.Error && message.Exception != null)
        {
            logger.LogCritical(message.Exception, "MqttPublish");
            Environment.Exit(1);
        }
    }

    private async Task Connect()
    {
        var clientOptionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(hostEnvironment.ApplicationName)
                .WithTcpServer("127.0.0.1")
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
                .WithCleanSession(true)
                .WithCleanStart(true);
        if (mqttWillProvider != null) 
        {
            clientOptionsBuilder
                .WithWillTopic(mqttWillProvider.WillTopic)
                .WithWillPayload(mqttWillProvider.WillPayload);
        }
        var managedClientOptions = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(3.5))
            .WithClientOptions(clientOptionsBuilder.Build()).
            Build();
        await mqttClient.StartAsync(managedClientOptions);
        logger.LogInformation("Started");
    }
    
    private async Task SubscribeTopic(string topic)
    {
        logger.LogInformation("Subscribing topic {0}", topic);
        var mqttSubscribeOptions = new MqttTopicFilterBuilder().WithTopic(topic).Build();
        await mqttClient.SubscribeAsync([mqttSubscribeOptions]);
    }

    /// <summary>
    /// Subscribe a raw binary MQTT topic 
    /// Doesn't support errors
    /// </summary>
    public async Task SubscribeRawTopic(string topic, Func<byte[], Task> handler)
    {
        await SubscribeTopic(topic);
        mqttClient.ApplicationMessageReceivedAsync += args =>
        {
            try
            {
                var msg = args.ApplicationMessage;
                if (msg.Topic == topic)
                {
                    args.IsHandled = true;
                    if (msg.PayloadSegment.Array != null)
                    {
                        _ = handler(msg.PayloadSegment.Array);
                    }
                    else 
                    {
                        _ = handler([]);
                    }
                }
                return Task.CompletedTask;
            }
            catch (Exception exc)
            {
                ReportException(exc);
                return Task.CompletedTask;
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
            await owner.SubscribeTopic(responseTopic);
            owner.mqttClient.ApplicationMessageReceivedAsync += args =>
            {
                try
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
                }
                catch (Exception exc)
                {
                    owner.ReportException(exc);
                    return Task.CompletedTask;
                }
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
        await SubscribeTopic(topic);
        mqttClient.ApplicationMessageReceivedAsync += args =>
        {
            try
            {
                var msg = args.ApplicationMessage;
                if (msg.Topic == topic && msg.ResponseTopic != null)
                {
                    args.IsHandled = true;
                    _ = ProcessRpc(msg, handler);
                }
                return Task.CompletedTask;
            }
            catch (Exception exc)
            {
                ReportException(exc);
                return Task.CompletedTask;
            }
        };
        logger.LogInformation("Created RPC endpoint, topic {0}", topic);
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
