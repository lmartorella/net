using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Lucky.HomeMock.Sinks;
using Lucky.Services;
using System.Threading;

namespace Lucky.HomeMock.Core
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class ControlPortListener : ServiceBaseWithData<Data>, ControlSession.IdProvider
    {
        private readonly TcpListener _serviceListener;
        private SinkMockBase[] _sinks;
        private CancellationToken _cancellationToken;
        private SinkMockBase[] _childSinks;

        public ControlPortListener()
        {
            Port = (ushort)new Random().Next(17000, 18000);

            IPAddress hostAddress = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));
            if (hostAddress == null)
            {
                hostAddress = new IPAddress(0x0100007f);
                LocalhostMode = true;
                Logger.Warning("Cannot find a public IP address of the host. Using loopback.");
            }

            while (_serviceListener == null)
            {
                try
                {
                    _serviceListener = new TcpListener(hostAddress, Port);
                    _serviceListener.Start();
                }
                catch (SocketException)
                {
                    Port++;
                    _serviceListener = null;
                }
            }
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

        public void InitSinks(IEnumerable<SinkMockBase> sinks, IEnumerable<SinkMockBase> childSinks)
        {
            _sinks = sinks.ToArray();
            _childSinks = childSinks.ToArray();
        }

        public ushort Port { get; private set; }
        public bool LocalhostMode { get; private set; }

        public Guid Id
        {
            get
            {
                return State.DeviceId;
            }
            set
            {
                State = new Data { DeviceId = value, ChildDeviceId = State.ChildDeviceId };
                Logger.Log("New guid: " + State.DeviceId);
            }
        }

        private class Child : ControlSession.IdProvider
        {
            private ControlPortListener _listener;

            public Child(ControlPortListener listener)
            {
                Logger = listener.Logger;
                _listener = listener;
            }

            public Guid Id
            {
                get
                {
                    return _listener.State.ChildDeviceId;
                }
                set
                {
                    _listener.State = new Data { DeviceId = _listener.State.DeviceId, ChildDeviceId = value };
                    Logger.Log("New guid: " + _listener.State.ChildDeviceId);
                }
            }

            public ILogger Logger { get; private set; }
        }


        public override void Dispose()
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

        private void HandleServiceSocketAccepted(TcpClient tcpClient)
        {
            tcpClient.NoDelay = true;
            using (var stream = tcpClient.GetStream())
            {
                Logger.Log("Incoming connection", "from", tcpClient.Client.RemoteEndPoint);

                // Now sends message
                using (var reader = new BinaryReader(stream))
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        var controlSession = new ControlSession("Master", writer, reader, this, _sinks);
                        controlSession.AddChild("Child", new Child(this), _childSinks);
                        Run(controlSession);
                    }
                }
            }
        }

        private void Run(ControlSession session)
        {
            try
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    if (session.RunServer() == ControlSession.RunStatus.Aborted)
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
