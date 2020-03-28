using Lucky.Home.Admin;
using Lucky.Home.Devices;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Lucky.Home.Services
{
    /// <summary>
    /// Connects to the server via TCP message-based channel
    /// </summary>
    internal class AdminClient : IAdminInterface
    {
        private Func<Stream> _streamProvider;
        private Action _disconnect;

        public AdminClient(Func<Stream> streamProvider, Action disconnect)
        {
            _streamProvider = streamProvider;
            _disconnect = disconnect;
        }

        private async Task<object> Request([CallerMemberName] string methodName = null, params object[] arguments)
        {
            MessageRequest request = new MessageRequest { Method = methodName, Arguments = arguments };
            try
            {
                var channel = new MessageChannel(_streamProvider());
                await Send(request, channel);
                return (await Receive(channel)).Value;
            }
            catch (Exception)
            {
                _disconnect();
                return null;
            }
        }

        private async Task Send(MessageRequest message, MessageChannel channel)
        {
            using (var ms = new MemoryStream())
            {
                MessageRequest.DataContractSerializer.WriteObject(ms, message);
                ms.Flush();
                await channel.WriteMessage(ms.ToArray());
            }
        }

        private async Task<MessageResponse> Receive(MessageChannel channel)
        {
            var buffer = await channel.ReadMessage();
            if (buffer == null)
            {
                return null;
            }
            using (var ms = new MemoryStream(buffer))
            {
                return (MessageResponse)MessageResponse.DataContractSerializer.ReadObject(ms);
            }
        }

        public async Task<Node[]> GetTopology()
        {
            return (Node[])await Request();
        }

        public async Task<bool> RenameNode(string nodeAddress, NodeId oldId, NodeId newId)
        {
            var ret = await Request("RenameNode", nodeAddress, oldId, newId);
            return (ret is bool) && (bool)ret;
        }

        public async Task ResetNode(NodeId id, string nodeAddress)
        {
            await Request("ResetNode", id, nodeAddress);
        }

        public async Task<string> CreateDevice(DeviceDescriptor descriptor)
        {
            return (string)await Request("CreateDevice", descriptor);
        }

        public async Task<DeviceDescriptor[]> GetDevices()
        {
            return (DeviceDescriptor[])await Request();
        }

        public async Task DeleteDevice(Guid id)
        {
            await Request("DeleteDevice", id);
        }
    }
}
