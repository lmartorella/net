using System;
using System.Net;
using System.Threading.Tasks;
using Lucky.Home.Core.Protocol;

namespace Lucky.Home.Core
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class NodeRegistrar : ServiceBase, INodeRegistrar
    {
        public NodeRegistrar()
            :base("NodeRegistrar")
        { }

        public INode LoginNode(Guid guid, IPAddress address)
        {
            var node = new Node(guid, address);
            // Start data fetch asynchrously
            node.FetchSinkData();
            return node;
        }

        public async Task<INode> RegisterBlankNode(IPAddress address)
        {
            var node = new Node(CreateNewGuid(), address);
            // Give the node the new name
            await node.Rename();
            // Start data fetch asynchrously
            node.FetchSinkData();
            return node;
        }

        private static Guid CreateNewGuid()
        {
            // Avoid 55aa string
            Guid ret;
            do
            {
                ret = Guid.NewGuid();
            } while (ret.ToString().ToLower().Contains("55aa") || ret.ToString().ToLower().Contains("55-aa"));
            return ret;
        }
    }
}
