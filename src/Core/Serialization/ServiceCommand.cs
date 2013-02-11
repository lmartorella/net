using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucky.Home.Core.Serialization
{
    class ServiceCommand
    {
        [SerializeAsCharArray(4)]
        public string Command;
    }

    class ServiceRegisterCommand
    {
        [SerializeAsDynArray]
        public SinkInfo[] Sinks;
    }

    class SinkInfo
    {
        public ushort DeviceId;
        public ushort DeviceCaps;
        public ushort Port;
    }

    class ServiceResponse
    {
        public ServerErrorCode ErrCode;
    }

    class ServiceResponseWithGuid : ServiceResponse
    {
        public Guid NewGuid;
    }
}
