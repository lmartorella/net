﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lucky.Home.Serialization;
using Lucky.Home.Sinks;
using Lucky.Services;
using System.Runtime.CompilerServices;
using Lucky.Home.Notification;

#pragma warning disable 649

namespace Lucky.Home.Protocol
{
    class TcpNode : ITcpNode
    {
        /// <summary>
        /// The Unique ID of the node, cannot be empty 
        /// </summary>
        public NodeId NodeId { get; private set; }

        /// <summary>
        /// If some active connection action was previously failed, and not yet restored by a heartbeat
        /// </summary>
        public bool IsZombie { get; set; }

        private readonly object _lockObject = new object();
        private bool _inFetchSinkData;
        private bool _inRename;
        private static readonly TimeSpan RetryTime = TimeSpan.FromSeconds(5);
        private TcpConnectionFactory _tcpConnectionFactory;

        /// <summary>
        /// Valid sinks
        /// </summary>
        private readonly List<SinkBase> _sinks = new List<SinkBase>();

        private Dictionary<int, ITcpNode> _lastKnownChildren = new Dictionary<int, ITcpNode>();
        private DateTime? _firstFailTime;

        private static readonly TimeSpan ZOMBIE_TIMEOUT = TimeSpan.FromSeconds(10);

        internal TcpNode(NodeId id, TcpNodeAddress address)
        {
            NodeId = id;
            Address = address;
            Logger = Manager.GetService<ILoggerFactory>().Create("Node:" + id);
            _tcpConnectionFactory = Manager.GetService<TcpConnectionFactory>();
        }

        public TcpNodeAddress Address { get; private set; }

        private ILogger Logger { get; set; }

        public async Task Heartbeat(TcpNodeAddress address)
        {
            // Update address!
            lock (Address)
            {
                Address = address;
            }
            Dezombie();

            // Check for zombied children and try to de-zombie it
            var lastKnownChildren = _lastKnownChildren.Values.ToArray();
            if (lastKnownChildren.Any(n => n == null || n.IsZombie))
            {
                // Re-fire the children request and de-zombie
                var subNodes = await GetChildrenIndexes();
                if (subNodes != null)
                {
                    // If same children, simply de-zombie
                    var sameChildren = lastKnownChildren.Select(n => n != null ? n.Address.Index : -1).SequenceEqual(subNodes);
                    if (sameChildren)
                    {
                        // Directly de-zombie all children
                        foreach (var node in lastKnownChildren.Where(c => c == null || c.IsZombie))
                        {
                            // Re-fetch reset status
                            await node.Relogin(node.Address);
                        }
                    }
                    else
                    {
                        // Else refetch metadata, something changed
                        await FetchMetadata();
                    }
                }
                // Else error in fetching children...
            }

            // Ask system sink if ETH node DEBUG DEBUG
            if (!address.IsSubNode)
            {
                var systemSink = Sink<SystemSink>();
                if (systemSink != null)
                {
                    // Ask system status
                    await systemSink.FetchStatus();
                }
            }
        }

        /// <summary>
        /// An already logged-in node relogs in (e.g. after node reset)
        /// </summary>
        public async Task Relogin(TcpNodeAddress address, int[] childrenChanged = null)
        {
            // Update address!
            lock (Address)
            {
                Address = address;
            }
            Dezombie();

            // Start data fetch asynchrously
            // This resets also the dirty children state
            await FetchMetadata();
            if (childrenChanged != null)
            {
                // Only fetch changed nodes AND present nodes
                var toRefecth = childrenChanged.Where(i => _lastKnownChildren.ContainsKey(i));

                var nodeManager = Manager.GetService<INodeManager>();
                // Register subnodes, asking for identity
                var children = await Task.WhenAll(toRefecth.Select(async i => await nodeManager.RegisterUnknownNode(address.SubNode(i))));
                children.Select(c => _lastKnownChildren[c.Address.Index] = c);
            }
        }

        private class GetChildrenMessage
        {
            [SerializeAsFixedString(2)]
            public string Cmd = "CH";
        }

        private class GetChildrenMessageResponse
        {
            public NodeId Id;

            // Number of children as bit array
            [SerializeAsDynArray]
            public byte[] Mask;
        }

        private class SelectNodeMessage
        {
            [SerializeAsFixedString(2)]
            public string Cmd = "SL";

            public short Index;
        }

        private class GetSinksMessage
        {
            [SerializeAsFixedString(2)] 
            public string Cmd = "SK";
        }

        private class NewGuidMessage
        {
            [SerializeAsFixedString(2)]
            public string Cmd = "GU";

            public NodeId Id;
        }

        private class GetSinksMessageResponse
        {
            [SerializeAsDynArray]
            [SerializeAsFixedString(4)]
            public string[] Sinks;
        }

        private class WriteDataMessage
        {
            [SerializeAsFixedString(2)]
            public string Cmd = "WR";

            public short SinkIndex;
        }

        private class ReadDataMessage
        {
            [SerializeAsFixedString(2)]
            public string Cmd = "RD";

            public short SinkIndex;
        }

        private class CloseMessage
        {
            [SerializeAsFixedString(2)]
            public string Cmd = "CL";
        }

        private class CloseMessageResponse
        {
            // 0x1e
            public byte Ack;
        }
        
        internal async Task FetchMetadata()
        {
            Logger.Log("Fetching metadata");
            lock (_lockObject)
            {
                if (_inFetchSinkData)
                {
                    return;
                }
                _inFetchSinkData = true;
            }

            Tuple<bool, TcpNodeAddress[]> ret;
            // Repeat until metadata are OK
            while (!(ret = await TryFetchMetadata()).Item1)
            {
                await Task.Delay(RetryTime);
            }

            // Ok, metadata of the master node are OK
            lock (_lockObject)
            {
                _inFetchSinkData = false;
            }

            // Now register all the children
            var nodeManager = Manager.GetService<INodeManager>();
            // Register subnodes, asking for identity
            var children = await Task.WhenAll(ret.Item2.Select(async address => await nodeManager.RegisterUnknownNode(address)));
            _lastKnownChildren.Clear();
            children.Select(c => _lastKnownChildren[c.Address.Index] = c);
        }

        private async Task<bool> OpenNodeSession(Func<ITcpConnection, TcpNodeAddress, Task<bool>> handler, [CallerMemberName] string context = null)
        {
            if (IsZombie)
            {
                // Avoid thrashing the network
                return false;
            }

            TcpNodeAddress address;
            lock (Address)
            {
                address = Address.Clone();
            }


            bool ok;
            var connection = _tcpConnectionFactory.Create(address.IPEndPoint);
            try
            {
                // Connecion can be recycled
                await connection.Write(new SelectNodeMessage { Index = (short)address.Index });
                DateTime dt = DateTime.Now;
                ok = await handler(connection, address);
                if (ok)
                {
                    await connection.Write(new CloseMessage());
                    var ack = await connection.Read<CloseMessageResponse>();
                    if (ack == null || ack.Ack != 0x1e)
                    {
                        // Forbicly close the channel
                        _tcpConnectionFactory.Abort(address.IPEndPoint);
                        Logger.Log("Missing CLOS response, elapsed: " + (DateTime.Now - dt).TotalMilliseconds.ToString("0.00") + "ms, in " + context);
                        ok = false;
                    }
                }
            }
            catch (Exception exc)
            {
                // Forbicly close the channel
                _tcpConnectionFactory.Abort(address.IPEndPoint);
                // Log exc
                Logger.Exception(exc);
                ok = false;
            }
            finally
            {
                connection.Dispose();
            }

            if (!ok)
            {
                if (_firstFailTime.HasValue)
                {
                    // Mark the node as a zombie after some time
                    if ((DateTime.Now - _firstFailTime) > ZOMBIE_TIMEOUT)
                    {
                        ToZombie();
                    }
                }
                else
                {
                    _firstFailTime = DateTime.Now;
                }
            }
            else
            {
                // Reset zombie time
                _firstFailTime = null;
            }

            return ok;
        }

        private void Dezombie()
        {
            if (IsZombie)
            {
                Logger.Log("Dezombie");
                IsZombie = false;
                Manager.GetService<INotificationService>().EnqueueStatusUpdate("Errori bus", "Risolto: ristabilita connessione con " + NodeId);
                UpdateSinks();
            }
        }

        private void ToZombie()
        {
            if (!IsZombie)
            {
                Logger.Log("IsZombie");
                IsZombie = true;
                Manager.GetService<INotificationService>().EnqueueStatusUpdate("Errori bus", "Errore: persa connessione con " + NodeId);
                UpdateSinks();
            }
        }

        private void UpdateSinks()
        {
            var sinkManager = Manager.GetService<SinkManager>();
            sinkManager.UpdateSinks();
        }

        private async Task<int[]> GetChildrenIndexes()
        {
            int[] ret = null;
            await OpenNodeSession(async (connection, addr) =>
            {
                await connection.Write(new GetChildrenMessage());
                var childNodes = await connection.Read<GetChildrenMessageResponse>();
                if (childNodes != null)
                {
                    ret = DecodeRawMask(childNodes.Mask, i => i);
                    return true;
                }
                else
                {
                    return false;
                }
            });
            return ret;
        }

        internal async Task<NodeId> TryFetchGuid()
        {
            // Init a METADATA fetch connection
            NodeId id = null;

            if (!await OpenNodeSession(async (connection, addr) =>
            {
                // Ask for subnodes
                await connection.Write(new GetChildrenMessage());
                var childNodes = await connection.Read<GetChildrenMessageResponse>();
                if (childNodes == null)
                {
                    // Channel already destroyed
                    return false;
                }
                id = childNodes.Id;
                return true;
            }))
            {
                // Error, no metadata
                return null;
            }

            return id;
        }

        private async Task<Tuple<bool, TcpNodeAddress[]>> TryFetchMetadata()
        {
            // Init a METADATA fetch connection
            string[] sinks = null;
            byte[] childMask = new byte[0];
            TcpNodeAddress address = null;

            if (!await OpenNodeSession(async (connection, addr) =>
            {
                address = addr;

                // Ask for subnodes
                await connection.Write(new GetChildrenMessage());
                var childNodes = await connection.Read<GetChildrenMessageResponse>();
                if (childNodes == null)
                {
                    return false;
                }

                if (!childNodes.Id.Equals(NodeId))
                {
                    // ERROR
                    Logger.Warning("InvalidGuidInEnum", "Id", NodeId, "returned", childNodes.Id);
                }
                childMask = childNodes.Mask;

                // Then ask for sinks
                await connection.Write(new GetSinksMessage());
                var response = await connection.Read<GetSinksMessageResponse>();
                if (response == null)
                {
                    return false;
                }
                sinks = response.Sinks;
                return true;
            }))
            {
                // Error, no metadata
                return Tuple.Create(false, new TcpNodeAddress[0]);
            }

            // Now register sinks
            RegisterSinks(sinks);

            return Tuple.Create(true, DecodeRawMask(childMask, i => address.SubNode(i)));
        }

        private static IEnumerable<int> ToIntEnumerable(byte[] mask)
        {
            int index = 0;
            for (int j = 0; j < mask.Length; j++)
            {
                var b = mask[j];
                for (int i = 0; i < 8; i++, index++, b >>= 1)
                {
                    if ((b & 1) != 0)
                    {
                        yield return index;
                    }
                }
            }
        }

        public static T[] DecodeRawMask<T>(byte[] mask, Func<int, T> select)
        {
            return ToIntEnumerable(mask).Select(i => select(i + 1)).ToArray();
        }

        private void RegisterSinks(string[] sinks)
        {
            Logger.Log("Registering sinks");
            var sinkManager = Manager.GetService<SinkManager>();

            // Identity of sink is due to its position in the sink array.
            // Null sink are valid, it means no sink at that position. Useful for dynamic add/remove of sinks.
            lock (_sinks)
            {
                ClearSinks();
                _sinks.AddRange(sinks.Select((s, i) => sinkManager.CreateSink(s, this, i)));
            }
        }

        private void ClearSinks()
        {
            var sinkManager = Manager.GetService<SinkManager>();

            lock (_sinks)
            {
                foreach (var sink in _sinks)
                {
                    sinkManager.DestroySink(sink);
                }
                _sinks.Clear();
            }
        }

        private void RenameInternal(NodeId newId)
        {
            NodeId oldId = NodeId;
            NodeId = newId;
            Manager.GetService<INodeManager>().BeginRenameNode(this, newId);
            Manager.GetService<INodeManager>().EndRenameNode(this, oldId, newId, true);

            Logger.Warning("CorrectGuidAssigned", "oldId", oldId, "newId", newId);
        }

        /// <summary>
        /// Change the ID of the node
        /// </summary>
        public async Task<bool> Rename(NodeId newId)
        {
            if (newId.IsEmpty)
            {
                throw new ArgumentNullException("newId");
            }
            lock (_lockObject)
            {
                if (_inRename)
                {
                    return false;
                }
                if (newId.Equals(NodeId))
                {
                    return true;
                }
                _inRename = true;
            }

            bool success = false;
            NodeId oldId = new NodeId();
            try
            {
                // Notify the node registrar too
                Manager.GetService<INodeManager>().BeginRenameNode(this, newId);

                await OpenNodeSession(async (connection, address) =>
                {
                    await connection.Write(new NewGuidMessage { Id = newId });
                    return true;
                });

                success = true;
                oldId = NodeId;
                NodeId = newId;
            }
            catch (Exception exc)
            {
                success = false;
                Logger.Exception(exc);
            }
            finally
            {
                lock (_lockObject)
                {
                    _inRename = false;
                }
                Manager.GetService<INodeManager>().EndRenameNode(this, oldId, newId, success);
            }
            return success;
        }

        public async Task<bool> WriteToSink(int sinkId, Func<IConnectionWriter, Task> writeHandler, [CallerMemberName] string context = "")
        {
            return await OpenNodeSession(async (connection, address) =>
            {
                // Open stream
                await connection.Write(new WriteDataMessage { SinkIndex = (short) sinkId });
                // Now the channel is owned by the sink driver
                // Returns when done, and the protocol should leave the channel clean
                await writeHandler(connection);
                return true;
            }, "WriteToSink:" + context);
        }

        public async Task<bool> ReadFromSink(int sinkId, Func<IConnectionReader, Task> readHandler, [CallerMemberName] string context = "")
        {
            return await OpenNodeSession(async (connection, address) =>
            {
                // Open stream
                await connection.Write(new ReadDataMessage {SinkIndex = (short) sinkId});
                // This is blocker call
                await readHandler(connection);
                return true;
            }, "ReadFromSink:" + context);
        }

        public T Sink<T>() where T : ISink
        {
            lock (_sinks)
            {
                return _sinks.OfType<T>().FirstOrDefault();
            }
        }

        public ISink[] Sinks
        {
            get
            {
                lock (_sinks)
                {
                    return _sinks.ToArray();
                }
            }
        }
    }
}
