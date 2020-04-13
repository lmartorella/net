using Lucky.Home.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Lucky.Home.Simulator
{
    /// <summary>
    /// Implements control message client protocol (used by TCP mock master node and simulated slave node as well)
    /// </summary>
    class ClientProtocolNode
    {
        private ILogger Logger;
        private readonly BinaryWriter _writer;
        private readonly BinaryReader _reader;
        public ISinkMock[] Sinks { get; private set; }
        private readonly List<ClientProtocolNode> _children = new List<ClientProtocolNode>();
        private readonly ISimulatedNode _node;
        private readonly HeloSender _heloSender;
        private readonly Dispatcher _dispatcher;

        public ClientProtocolNode(Dispatcher dispatcher, BinaryWriter writer, BinaryReader reader, ISimulatedNodeInternal node, ISinkMock[] sinks, HeloSender heloSender = null)
        {
            Logger = Manager.GetService<ILoggerFactory>().Create("ClientProtocolNode", node.Id.ToString());
            node.IdChanged += (o, e) => Logger.SubKey = node.Id.ToString();
            _dispatcher = dispatcher;
            _node = node;
            _writer = writer;
            _reader = reader;
            _heloSender = heloSender;
            Sinks = sinks;
        }

        public void AddChild(string name, SlaveNode slaveNode)
        {
            _children.Add(new ClientProtocolNode(_dispatcher, _writer, _reader, slaveNode, slaveNode.Sinks));
        }

        private string ReadCommand()
        {
            var buffer = new byte[2];
            int idx = 0;
            do
            {
                int c = _reader.Read(buffer, idx, 2 - idx);
                if (c == 0)
                {
                    return null;
                }
                idx += c;
            } while (idx < 2);
            return Encoding.ASCII.GetString(buffer);
        }

        public enum RunStatus
        {
            Continue,
            Closed,
            Aborted
        }

        public async Task<RunStatus> RunServer()
        {
            string command = ReadCommand();
            if (command == null)
            {
                return RunStatus.Aborted;
            }

            string msg = "Msg: " + command;
            var sinkManager = Manager.GetService<MockSinkManager>();
            ushort sinkIdx;
            switch (command)
            {
                case "CL":
                    // Ack
                    Logger.Log(msg);
                    _writer.Write(new byte[] { 0x1e });
                    return RunStatus.Closed;
                case "CH":
                    Write(_node.Id.ToBytes());
                    Write((ushort)_children.Count);
                    if (_children.Count > 0) {
                        // 1 bit for each children
                        if (_children.Count > 8)
                        {
                            throw new NotImplementedException();
                        }
                        byte r = 1;
                        for (int i = 1; i < _children.Count; i++)
                        {
                            r <<= 1;
                            r |= 1;
                        }
                        Write(r);
                    }

                    break;
                case "SL":
                    var id = ReadUint16();
                    msg += " " + id;
                    if (id > 0)
                    {
                        while (true)
                        {
                            var ret = await _children[id - 1].RunServer();
                            if (ret == RunStatus.Closed)
                            {
                                _heloSender.ChildChanged = false;
                                break;
                            }
                            else if (ret == RunStatus.Aborted)
                            {
                                return RunStatus.Aborted;
                            }
                        }
                    }
                    break;
                case "SK":
                    Write((ushort)Sinks.Length);
                    foreach (var sink in Sinks)
                    {
                        Write(sinkManager.GetFourCc(sink));
                    }
                    break;
                case "GU":
                    _node.Id = NodeId.FromBytes(ReadBytes(16));
                    msg += " " + _node.Id;
                    break;
                case "WR":
                    sinkIdx = ReadUint16();
                    msg += " " + sinkIdx + " (" + Sinks[sinkIdx].GetFourCc() + ")";
                    // Sync with main dispatcher
                    _dispatcher.Invoke(() =>
                    {
                        Sinks[sinkIdx].Read(_reader);
                    });
                    break;
                case "RD":
                    sinkIdx = ReadUint16();
                    msg += " " + sinkIdx + " (" + Sinks[sinkIdx].GetFourCc() + ")";
                    _dispatcher.Invoke(() =>
                    {
                        Sinks[sinkIdx].Write(_writer);
                    });
                    break;
                default:
                    throw new InvalidOperationException("Unknown protocol command: " + command);
            }
            Logger.Log(msg);
            return RunStatus.Continue;
        }

        private ushort ReadUint16()
        {
            return _reader.ReadUInt16();
        }

        private byte[] ReadBytes(int size)
        {
            return _reader.ReadBytes(size);
        }

        private void Write(ushort i)
        {
            _writer.Write(BitConverter.GetBytes(i));
        }

        private void Write(byte i)
        {
            _writer.Write(i);
        }

        private void Write(string data)
        {
            _writer.Write(Encoding.ASCII.GetBytes(data));
        }

        private void Write(byte[] bytes)
        {
            _writer.Write(bytes);
        }
    }
}
