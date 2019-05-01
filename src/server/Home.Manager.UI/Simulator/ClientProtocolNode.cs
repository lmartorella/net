using Lucky.Home.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
        private ISimulatedNode _node;
        private string _logId;
        private HeloSender _heloSender;

        public ClientProtocolNode(string logId, BinaryWriter writer, BinaryReader reader, ISimulatedNode idProvider, ISinkMock[] sinks, HeloSender heloSender = null)
        {
            _logId = logId;
            Logger = Manager.GetService<ILoggerFactory>().Create("ClientProtocolNode:" + logId);
            _node = idProvider;
            _writer = writer;
            _reader = reader;
            _heloSender = heloSender;
            Sinks = sinks;
        }

        public void AddChild(string name, SlaveNode slaveNode)
        {
            _children.Add(new ClientProtocolNode(name, _writer, _reader, slaveNode, slaveNode.Sinks));
        }

        private string ReadCommand()
        {
            var buffer = MasterNode.ReadBytesWait(_reader, 2);
            if (buffer == null)
            {
                return null;
            }
            return Encoding.ASCII.GetString(buffer);
        }

        public enum RunStatus
        {
            Continue,
            Closed,
            Aborted
        }

        public RunStatus RunServer()
        {
            string command = ReadCommand();
            if (command == null)
            {
                return RunStatus.Aborted;
            }

            Logger.Log("Msg: " + command);
            var sinkManager = Manager.GetService<MockSinkManager>();
            ushort sinkIdx;
            switch (command)
            {
                case "CL":
                    // Ack
                    _writer.Write(new byte[] { 0x1e });
                    return RunStatus.Closed;
                case "CH":
                    Write(_node.Id);
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
                    if (id > 0)
                    {
                        while (true)
                        {
                            var ret = _children[id - 1].RunServer();
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
                    _node.Id = ReadGuid();
                    break;
                case "WR":
                    sinkIdx = ReadUint16();
                    Sinks[sinkIdx].Read(_reader);
                    break;
                case "RD":
                    sinkIdx = ReadUint16();
                    Sinks[sinkIdx].Write(_writer);
                    break;
                default:
                    throw new InvalidOperationException("Unknown protocol command: " + command);
            }

            return RunStatus.Continue;
        }

        private ushort ReadUint16()
        {
            return _reader.ReadUInt16();
        }

        private Guid ReadGuid()
        {
            return new Guid(_reader.ReadBytes(16));
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

        private void Write(Guid guid)
        {
            _writer.Write(guid.ToByteArray());
        }
    }
}
