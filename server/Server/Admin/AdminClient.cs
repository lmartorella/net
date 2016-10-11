using Lucky.Home.Devices;
using Lucky.IO;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Lucky.Home.Admin
{
    public class AdminClient : IAdminInterface
    {
        private MessageChannel _channel;
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
                using (_channel = new MessageChannel(_streamProvider()))
                {
                    await Send(request);
                    return (await Receive()).Value;
                }
            }
            catch (Exception)
            {
                _disconnect();
                return null;
            }
        }

        private async Task Send(MessageRequest message)
        {
            using (var ms = new MemoryStream())
            {
                MessageRequest.DataContractSerializer.WriteObject(ms, message);
                ms.Flush();
                await _channel.WriteMessage(ms.ToArray());
            }
        }

        private async Task<MessageResponse> Receive()
        {
            var buffer = await _channel.ReadMessage();
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

        public async Task<DeviceTypeDescriptor[]> GetDeviceTypes()
        {
            return (DeviceTypeDescriptor[])await Request();
        }

        public async Task<bool> RenameNode(string nodeAddress, Guid oldId, Guid newId)
        {
            return (bool)await Request("RenameNode", nodeAddress, oldId, newId);
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
