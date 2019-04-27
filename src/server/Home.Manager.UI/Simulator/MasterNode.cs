using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Lucky.Home.Services;
using System.Threading;

namespace Lucky.Home.Simulator
{
    public class MasterNode : ISimulatedNode, IDisposable
    {
        private readonly TcpListener _serviceListener;
        private ISinkMock[] _sinks;
        private CancellationToken _cancellationToken;
        private HeloSender _heloSender;
        public ILogger Logger { get; private set; }
        public IStateProvider StateProvider { get; private set; }
        private List<SlaveNode> _children = new List<SlaveNode>();

        public MasterNode(ILogger logger, IStateProvider stateProvider, string[] sinks)
        {
            Logger = logger;
            StateProvider = stateProvider;
            var sinkManager = Manager.GetService<MockSinkManager>();
            _sinks = sinks.Select(name => sinkManager.Create(name, this)).ToArray();

            var port = (ushort)new Random().Next(17000, 18000);
            bool localhostMode = false;

            IPAddress hostAddress = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));
            if (hostAddress == null)
            {
                hostAddress = new IPAddress(0x0100007f);
                localhostMode = true;
                Logger.Warning("Cannot find a public IP address of the host. Using loopback.");
            }

            while (_serviceListener == null)
            {
                try
                {
                    _serviceListener = new TcpListener(hostAddress, port);
                    _serviceListener.Start();
                }
                catch (SocketException)
                {
                    port++;
                    _serviceListener = null;
                }
            }
            Logger.Log("Listen", "Port", port);

            _heloSender = new HeloSender(port, localhostMode, this);
        }

        public void StartServer(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            System.Threading.Tasks.Task.Run(async () =>
            {
                while (true)
                {
                    var tcpClient = await _serviceListener.AcceptTcpClientAsync();
                    HandleServiceSocketAccepted(tcpClient);
                }
            }, cancellationToken);
        }

        public Guid Id
        {
            get
            {
                return StateProvider.Id;
            }
            set
            {
                StateProvider.Id = value;
            }
        }

        public void Dispose()
        {
            _serviceListener.Stop();
        }

        internal static byte[] ReadBytesWait(BinaryReader reader, int l)
        {
            var buffer = new byte[l];
            int idx = 0;
            do
            {
                int c = reader.Read(buffer, idx, l - idx);
                if (c == 0)
                {
                    return null;
                }
                idx += c;
            } while (idx < l);
            return buffer;
        }

        public void AddChild(SlaveNode node)
        {
            _children.Add(node);
            _heloSender.ChildChanged = false;
        }

        private void HandleServiceSocketAccepted(TcpClient tcpClient)
        {
            var sinkManager = Manager.GetService<MockSinkManager>();
            tcpClient.NoDelay = true;
            using (var stream = tcpClient.GetStream())
            {
                Logger.Log("Incoming connection", "from", tcpClient.Client.RemoteEndPoint);

                // Now sends message
                using (var reader = new BinaryReader(stream))
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        var controlSession = new ClientProtocolNode("Master", writer, reader, this, _sinks, _heloSender);
                        _children.ForEach(child =>
                        {
                            controlSession.AddChild("Child", child);
                        });
                        Run(controlSession);
                    }
                }
            }
        }

        private void Run(ClientProtocolNode session)
        {
            try
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    if (session.RunServer() == ClientProtocolNode.RunStatus.Aborted)
                    {
                        break;
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.Exception(exc);
            }
        }
    }
}
