using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lucky.Home.Serialization;
using Lucky.Home.Sinks;
using Lucky.Services;
using System.Runtime.CompilerServices;

#pragma warning disable 414
#pragma warning disable 649
#pragma warning disable 169
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable NotAccessedField.Local

namespace Lucky.Home.Protocol
{
    class TcpNode : ITcpNode
    {
        /// <summary>
        /// The Unique ID of the node, cannot be empty 
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// If some active connection action was previously failed, and not yet restored by a heartbeat
        /// </summary>
        public bool IsZombie { get; set; }

        private readonly object _lockObject = new object();
        private bool _inFetchSinkData;
        private bool _inRename;
        private static readonly TimeSpan RetryTime = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Valid sinks
        /// </summary>
        private readonly List<SinkBase> _sinks = new List<SinkBase>();

        private Dictionary<int, ITcpNode> _lastKnownChildren = new Dictionary<int, ITcpNode>();
        private DateTime? _firstFailTime;

        private static readonly TimeSpan ZOMBIE_TIMEOUT = TimeSpan.FromSeconds(10);

        internal TcpNode(Guid guid, TcpNodeAddress address)
        {
            Id = guid;
            Address = address;
            Logger = Manager.GetService<ILoggerFactory>().Create("Node:" + guid);
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
            IsZombie = false;

            // Check for zombied children and try to de-zombie it
            var lastKnownChildren = _lastKnownChildren.Values.ToArray();
            if (lastKnownChildren.Any(n => n == null || n.IsZombie))
            {
                // Re-fire the children request and de-zombie
                var subNodes = GetChildrenIndexes();
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
                // Ask system status
                await Sink<SystemSink>().FetchStatus();
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
            IsZombie = false;

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
            public Guid Guid;

            // Number of children as bit array
            [SerializeAsDynArray]
            public byte[] Mask;
        }

        private class SelectNodeMessage
        {
            [SerializeAsFixedString(2)]
            public string Cmd = "SL";

            public short Index;

            public SelectNodeMessage(int index)
            {
                Index = (short)index;
            }
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

            public readonly Guid Guid;

            public NewGuidMessage(Guid guid)
            {
                Guid = guid;
            }
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
            while (!(ret = TryFetchMetadata()).Item1)
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

        private static bool OpenNodeSession(ILogger logger, TcpNodeAddress address, Func<TcpConnection, TcpNodeAddress, bool> handler, [CallerMemberName] string context = null)
        {
            var tcpConnectionFactory = Manager.GetService<TcpConnectionFactory>();

            // Init a METADATA fetch connection
            try
            {
                using (var connection = tcpConnectionFactory.Create(address.IPEndPoint))
                {
                    // Connecion can be recycled
                    connection.Write(new SelectNodeMessage(address.Index));
                    var ret = handler(connection, address);
                    if (ret)
                    {
                        DateTime dt = DateTime.Now;
                        connection.Write(new CloseMessage());
                        var ack = connection.Read<CloseMessageResponse>();
                        if (ack == null || ack.Ack != 0x1e)
                        {
                            // Forbicly close the channel
                            tcpConnectionFactory.Abort(address.IPEndPoint);
                            logger.Log("Missing CLOS response, elapsed: " + (DateTime.Now - dt).TotalMilliseconds.ToString("0.00") + "ms, in " + context);
                            return false;
                        }
                    }
                    return ret;
                }
            }
            catch (Exception exc)
            {
                // Forbicly close the channel
                tcpConnectionFactory.Abort(address.IPEndPoint);
                // Log exc
                logger.Exception(exc);
                return false;
            }
        }

        private bool OpenNodeSession(Func<TcpConnection, TcpNodeAddress, bool> handler, [CallerMemberName] string context = null)
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

            var ret = OpenNodeSession(Logger, address, handler, context);
            if (!ret)
            {
                if (_firstFailTime.HasValue)
                {
                    // Mark the node as a zombie after some time
                    if ((DateTime.Now - _firstFailTime) > ZOMBIE_TIMEOUT)
                    {
                        IsZombie = true;
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
            return ret;
        }

        private int[] GetChildrenIndexes()
        {
            int[] ret = null;
            OpenNodeSession((connection, addr) =>
            {
                connection.Write(new GetChildrenMessage());
                var childNodes = connection.Read<GetChildrenMessageResponse>();
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

        internal Guid? TryFetchGuid()
        {
            // Init a METADATA fetch connection
            Guid? guid = null;

            if (!OpenNodeSession((connection, addr) =>
            {
                // Ask for subnodes
                connection.Write(new GetChildrenMessage());
                var childNodes = connection.Read<GetChildrenMessageResponse>();
                if (childNodes == null)
                {
                    // Channel already destroyed
                    return false;
                }
                guid = childNodes.Guid;
                return true;
            }))
            {
                // Error, no metadata
                return null;
            }

            return guid;
        }

        private Tuple<bool, TcpNodeAddress[]> TryFetchMetadata()
        {
            // Init a METADATA fetch connection
            string[] sinks = null;
            byte[] childMask = new byte[0];
            TcpNodeAddress address = null;
            Guid newGuidToAssign = Guid.Empty;

            if (!OpenNodeSession((connection, addr) =>
            {
                address = addr;

                // Ask for subnodes
                connection.Write(new GetChildrenMessage());
                var childNodes = connection.Read<GetChildrenMessageResponse>();
                if (childNodes == null)
                {
                    return false;
                }

                if (childNodes.Guid != Id)
                {
                    // ERROR
                    Logger.Warning("InvalidGuidInEnum", "Id", Id, "returned", childNodes.Guid);
                }
                childMask = childNodes.Mask;

                // Then ask for sinks
                connection.Write(new GetSinksMessage());
                var response = connection.Read<GetSinksMessageResponse>();
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

            if (newGuidToAssign != Guid.Empty)
            {
                // Rename node
                RenameInternal(newGuidToAssign);
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

        private void RenameInternal(Guid newId)
        {
            Guid oldId = Id;
            Id = newId;
            Manager.GetService<INodeManager>().BeginRenameNode(this, newId);
            Manager.GetService<INodeManager>().EndRenameNode(this, oldId, newId, true);

            Logger.Warning("CorrectGuidAssigned", "oldId", oldId, "newId", newId);
        }

        /// <summary>
        /// Change the ID of the node
        /// </summary>
        public bool Rename(Guid newId)
        {
            if (newId == Guid.Empty)
            {
                throw new ArgumentNullException("newId");
            }
            lock (_lockObject)
            {
                if (_inRename)
                {
                    return false;
                }
                if (newId == Id)
                {
                    return true;
                }
                _inRename = true;
            }

            bool success = false;
            Guid oldId = Guid.Empty;
            try
            {
                // Notify the node registrar too
                Manager.GetService<INodeManager>().BeginRenameNode(this, newId);

                OpenNodeSession((connection, address) =>
                {
                    connection.Write(new NewGuidMessage(newId));
                    return true;
                });

                success = true;
                oldId = Id;
                Id = newId;
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

        public bool WriteToSink(int sinkId, Action<IConnectionWriter> writeHandler, [CallerMemberName] string context = "")
        {
            return OpenNodeSession((connection, address) =>
            {
                // Open stream
                connection.Write(new WriteDataMessage { SinkIndex = (short) sinkId });
                // Now the channel is owned by the sink driver
                // Returns when done, and the protocol should leave the channel clean
                writeHandler(connection);
                return true;
            }, "WriteToSink:" + context);
        }

        public bool ReadFromSink(int sinkId, Action<IConnectionReader> readHandler)
        {
            return OpenNodeSession((connection, address) =>
            {
                // Open stream
                connection.Write(new ReadDataMessage {SinkIndex = (short) sinkId});
                readHandler(connection);
                return true;
            });
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
