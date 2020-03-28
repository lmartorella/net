using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Lucky.Home.Services;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Lucky.Home.Simulator
{
    class MasterNode : NodeBase, IDisposable
    {
        private readonly TcpListener _serviceListener;
        private HeloSender _heloSender;
        private List<SlaveNode> _children = new List<SlaveNode>();
        private Dispatcher _dispatcher;

        private CancellationTokenSource _cancellationTokenSrc = new CancellationTokenSource();
        private CancellationToken _cancellationToken;

        public MasterNode(Dispatcher dispatcher, SimulatorNodesService.NodeData nodeData)
            :base("MasterNode", nodeData)
        {
            _dispatcher = dispatcher;
 
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

        public SlaveNode[] Children
        {
            get
            {
                return _children.ToArray();
            }
        }

        public void StartServer()
        {
            _cancellationToken = _cancellationTokenSrc.Token;
            TcpClient tcpClient = null;
            Task.Run(async () =>
            {
                while (true)
                {
                    tcpClient = await _serviceListener.AcceptTcpClientAsync();
                    _ = HandleServiceSocketAccepted(tcpClient);
                }
            }, _cancellationToken).ContinueWith(t =>
            {
                if (tcpClient != null)
                {
                    tcpClient.Close();
                    tcpClient.Dispose();
                }
            }, TaskContinuationOptions.OnlyOnCanceled);
        }

        public void Dispose()
        {
            _serviceListener.Stop();
        }

        public void AddChild(SlaveNode node)
        {
            _children.Add(node);
            _heloSender.ChildChanged = false;
        }

        private async Task HandleServiceSocketAccepted(TcpClient tcpClient)
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
                        var controlSession = new ClientProtocolNode(_dispatcher, writer, reader, this, Sinks, _heloSender);
                        _children.ForEach(child =>
                        {
                            controlSession.AddChild("Child", child);
                        });
                        await Run(controlSession);
                    }
                }
            }
        }

        private async Task Run(ClientProtocolNode session)
        {
            try
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    if (await session.RunServer() == ClientProtocolNode.RunStatus.Aborted)
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

        public override void Reset()
        {
            _cancellationTokenSrc.Cancel();
            _cancellationTokenSrc = new CancellationTokenSource();
            StartServer();
        }
    }
}
