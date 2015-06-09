//using System;
//using Lucky.Home.Sinks;

//namespace Lucky.Home.Core.Serialization
//{
//    public class ServiceCommand
//    {
//        [SerializeAsFixedArray(4)]
//        [Selector("RGST", typeof(ServiceRegisterCommand))]
//        public string Command;
//    }

//    public class ServiceRegisterCommand : ServiceCommand
//    {
//        [SerializeAsDynArray]
//        public SinkInfo[] Sinks;
//    }

//    public class SinkInfo
//    {
//        public SinkTypes SinkType;
//        public ushort DeviceCaps;
//        public ushort Port;
//    }

//    class ServiceResponse
//    {
//        public ServerErrorCode ErrCode;
//    }

//    class ServiceResponseWithGuid : ServiceResponse
//    {
//        public Guid NewGuid;
//    }
//}
