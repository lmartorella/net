using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lucky.Home.Serialization;
using Lucky.Home.Sinks;
using Lucky.Home.Services;
using System.Runtime.CompilerServices;

#pragma warning disable 649

namespace Lucky.Home.Protocol
{
    /// <summary>
    /// A TCP node (master node)
    /// </summary>
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
        private bool _inRename;
        private static readonly TimeSpan RetryTime = TimeSpan.FromSeconds(5);
        private TcpConnectionSessionManager _tcpConnectionFactory;

        /// <summary>
        /// Valid sinks
        /// </summary>
        private readonly List<SinkBase> _sinks = new List<SinkBase>();

        private List<ITcpNode> _lastKnownChildren = new List<ITcpNode>();
        private DateTime? _firstFailTime;
        private readonly E2EStatLogger _e2eStatLogger;

        // 20 seconds is the default TCP timeout time. Let's wait for some more before sending notifications about zombification...
        private static readonly TimeSpan ZOMBIE_TIMEOUT = TimeSpan.FromSeconds(30);

        internal TcpNode(NodeId id, TcpNodeAddress address)
        {
            NodeId = id;
            Address = address;
            Logger = Manager.GetService<ILoggerFactory>().Create("Node:" + id);
            _tcpConnectionFactory = Manager.GetService<TcpConnectionSessionManager>();
            _e2eStatLogger = new E2EStatLogger(id);
        }

        public TcpNodeAddress Address { get; private set; }

        private ILogger Logger { get; set; }

        public async Task Heartbeat(TcpNodeAddress address, int[] aliveChildren)
        {
            var wasZombie = Dezombie("hbeat", address);
            if (wasZombie)
            {
                // Refetch metadata, something can be changed
                await FetchMetadata("heartbeat");
            }
            else
            {
                // Check for zombied children and try to de-zombie it
                ITcpNode[] lastKnownChildren;
                lock (_lockObject)
                {
                    lastKnownChildren = _lastKnownChildren.ToArray();
                }
                if (lastKnownChildren.Any(n => n.IsZombie))
                {
                    // Refetch metadata and children, something changed
                    await FetchMetadata("heartbeat");
                }
                else
                {
                    // Do children check
                    var lastKnownChildrenIndexes = lastKnownChildren.Select(n => n.Address.Index);
                    if (aliveChildren != null && !lastKnownChildrenIndexes.SequenceEqual(aliveChildren))
                    {
                        Func<IEnumerable<int>, string> ToString = coll =>
                        {
                            return string.Join(";", coll.Select(i => i.ToString()));
                        };
                        Logger.Log("DiffChildInHtbt", "mine", ToString(lastKnownChildrenIndexes), "rcvd", ToString(aliveChildren));
                        // Refetch metadata and children, something changed
                        await FetchMetadata("heartbeat");
                    }
                }
            }

            // Ask system sink after dezombie
            if (wasZombie && !IsZombie)
            {
                await FetchSystemStatus();
            }
        }

        private async Task FetchSystemStatus()
        {
            var systemSink = Sink<SystemSink>();
            if (systemSink != null)
            {
                // Ask system status
                await systemSink.FetchStatus();
            }
        }

        /// <summary>
        /// An already logged-in node relogs in (e.g. after node reset)
        /// </summary>
        public async Task Relogin(TcpNodeAddress address, int[] childrenChanged = null)
        {
            Dezombie("relogin: " + address.ToString() + ", childrenChanged: " + ((childrenChanged != null) ? string.Join(";", childrenChanged.Select(c => c.ToString())) : "<null>"), address);

            // Start data fetch asynchronously
            // This resets also the dirty children state. This will fetch the reset status as well
            await FetchMetadata("relogin");

            if (childrenChanged != null && !IsZombie)
            {
                var nodeManager = Manager.GetService<NodeManager>();

                int[] knownIdxs;
                lock (_lockObject)
                {
                    knownIdxs = _lastKnownChildren.Select(c => c.Address.Index).ToArray();
                }
                // Only fetch changed nodes AND present nodes
                var toRefecth = childrenChanged.Where(i => knownIdxs.Contains(i));
                var deadNodesIndices = childrenChanged.Where(i => !knownIdxs.Contains(i));
                foreach (var idx in deadNodesIndices)
                {
                    Logger.Log("DeadNode", "address", address, "idx", idx);
                }

                await RegisterChildren(toRefecth.Select(i => address.SubNode(i)), "relogin");
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

        private bool _inFetchSinkData;
        /// <summary>
        /// Fetch metadata and rebuild the children list
        /// </summary>
        internal async Task FetchMetadata(string context)
        {
            lock (_lockObject)
            {
                if (_inFetchSinkData)
                {
                    return;
                }
                _inFetchSinkData = true;
            }

            try
            {
                Logger.Log("Fetching metadata");

                Tuple<bool, TcpNodeAddress[]> ret;
                // Repeat until metadata are OK
                int i = 0;
                while (!(ret = await TryFetchMetadata()).Item1)
                {
                    if (++i > 10)
                    {
                        ToZombie();
                        return;
                    }
                    Logger.Log("RetryMD", "#", i);
                    await Task.Delay(RetryTime);
                }

                lock (_lockObject)
                {
                    _lastKnownChildren.Clear();
                }
                if (ret.Item2.Length > 0)
                {
                    // Now register all the children
                    Logger.Log("RegisterChildren", "count", ret.Item2.Length);
                    await RegisterChildren(ret.Item2, context);
                }
            }
            finally
            {
                _inFetchSinkData = false;
            }
        }

        private bool _isRegisterChildren = false;
        /// <summary>
        /// Register children, one at a time to avoid timeouts on the master line
        /// </summary>
        private async Task RegisterChildren(IEnumerable<TcpNodeAddress> addresses, string context)
        {
            lock (_lockObject)
            {
                if (_isRegisterChildren)
                {
                    return;
                }
                _isRegisterChildren = true;
            }

            try
            {
                var nodeManager = Manager.GetService<NodeManager>();

                // Register subnodes, asking for identity
                foreach (var address in addresses)
                {
                    var child = await nodeManager.RegisterUnknownNode(address, context);
                    if (child != null)
                    {
                        lock (_lockObject)
                        {
                            _lastKnownChildren.Add(child);
                        }
                    }
                }
            }
            finally
            {
                _isRegisterChildren = false;
            }
        }

        private async Task<bool> OpenNodeSession(Func<TcpConnectionSession, TcpNodeAddress, Task<bool>> handler, bool dezombie = false, [CallerMemberName] string context = null)
        {
            if (!dezombie && IsZombie)
            {
                // Avoid thrashing the network
                return false;
            }

            TcpNodeAddress address;
            lock (Address)
            {
                address = Address.Clone();
            }

            bool ok = false;
            var connection = _tcpConnectionFactory.GetConnection(address.IPEndPoint);
            // Connection can be made?
            if (connection != null)
            {
                try
                {
                    // Connecion can be recycled, do selection
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
                            connection.Abort("noclose");
                            Logger.Log("Missing CLOS response, elapsed: " + (DateTime.Now - dt).TotalMilliseconds.ToString("0.00") + "ms, in " + context);
                            ok = false;
                        }
                    }
                }
                catch (Exception exc)
                {
                    // Log exc
                    Logger.Exception(exc);
                    // Forbicly close the channel
                    connection.Abort("exc");
                    ok = false;
                }
                finally
                {
                    connection.Dispose();
                }
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

        /// <summary>
        /// Returns true if the node was actually de-zombied
        /// </summary>
        internal bool Dezombie(string reason, TcpNodeAddress address)
        {
            // Update address
            lock (Address)
            {
                if (Address.IsSubNode != address.IsSubNode)
                {
                    Logger.Log("ERR:LevelChange", "from", Address, "to", address);
                }
                Address = address;
            }

            if (IsZombie)
            {
                Logger.Log("Dezombie", "reason", reason, "address", address);
                IsZombie = false;
                Manager.GetService<INotificationService>().EnqueueStatusUpdate("Errori bus", "Risolto: ristabilita connessione con " + NodeId);
                UpdateSinks();
                return true;
            }
            else
            {
                return false;
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
            Manager.GetService<SinkManager>().UpdateSinks();
        }

        /// <summary>
        /// Fetch and return the list of children addresses, +1 base
        /// </summary>
        private async Task<int[]> GetChildrenIndexes()
        {
            int[] ret = null;
            await OpenNodeSession(async (connection, addr) =>
            {
                await connection.Write(new GetChildrenMessage());
                var childNodes = await connection.Read<GetChildrenMessageResponse>();
                if (childNodes != null)
                {
                    ret = DecodeRawMask(childNodes.Mask, i => i + 1).ToArray();
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
            // Init a METADATA fetch connection, ignoring the zombie status
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
            }, true))
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
                if (childMask.Any(b => b != 0))
                {
                    Logger.Log("ChildMask", "mask", string.Join(",", DecodeRawMask(childMask, i => i.ToString())));
                }

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
            await RegisterSinks(sinks);

            // Create child addresses
            var subNodes = DecodeRawMask(childMask, i => address.SubNode(i + 1)).ToArray();
            return Tuple.Create(true, subNodes);
        }

        public static IEnumerable<T> DecodeRawMask<T>(byte[] mask, Func<int, T> select)
        {
            int index = 0;
            for (int j = 0; j < mask.Length; j++)
            {
                var b = mask[j];
                for (int i = 0; i < 8; i++, index++, b >>= 1)
                {
                    if ((b & 1) != 0)
                    {
                        yield return select(index);
                    }
                }
            }
        }

        private async Task RegisterSinks(string[] sinks)
        {
            var sinkManager = Manager.GetService<SinkManager>();

            // Identity of sink is due to its position in the sink array.
            // Null sink are valid, it means no sink at that position. Useful for dynamic add/remove of sinks.
            bool fetchSystemStatus = false;
            lock (_sinks)
            {
                // if sinks are not changed, don't recreate it
                if (_sinks.Select(s => s.FourCc).SequenceEqual(sinks))
                {
                    Logger.Log("SinksOk", "sinkIds", string.Join(",", sinks.Select(s => s.Trim())));
                    // Don't recreate the sinks. However ask for status errors...
                    fetchSystemStatus = true;
                }
                else
                {
                    Logger.Log("Registering sinks", "sinkIds", string.Join(",", sinks.Select(s => s.Trim())));
                    ClearSinks();
                    _sinks.AddRange(sinks.Select((s, i) => sinkManager.CreateSink(s, this, i)));
                }
            }
            
            if (fetchSystemStatus)
            {
                await FetchSystemStatus();
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
                Manager.GetService<NodeManager>().BeginRenameNode(this, newId);

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
                Manager.GetService<NodeManager>().EndRenameNode(this, oldId, newId, success);
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
            }, false, "WriteToSink:" + context);
        }

        public async Task<bool> ReadFromSink(int sinkId, Func<IConnectionReader, Task> readHandler, int timeout = 0, [CallerMemberName] string context = "")
        {
            var task = OpenNodeSession(async (connection, address) =>
            {
                // statistics, e2e
                DateTime start = DateTime.Now;
                // Open stream
                await connection.Write(new ReadDataMessage {SinkIndex = (short) sinkId});
                await readHandler(connection);

                // Take end time
                DateTime end = DateTime.Now;
                _e2eStatLogger.AddE2EReadSample(end - start);

                return true;
            }, false, "ReadFromSink:" + context);

            if (timeout > 0)
            {
                var t = await Task.WhenAny(task, Task.Delay(timeout));
                if (t == task)
                {
                    return task.Result;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return await task;
            }
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
