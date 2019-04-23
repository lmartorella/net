//using System;
//using System.Threading;
//using Lucky.Home.Core;
//using Lucky.Home.Core.Serialization;

//// ReSharper disable once UnusedMember.Global
//// ReSharper disable once MemberCanBeProtected.Global
//// ReSharper disable once MemberCanBePrivate.Global
//// ReSharper disable once NotAccessedField.Global

//namespace Lucky.Home.Sinks
//{
//    /// <summary>
//    /// Flash a new boot ROM for 18f87j60
//    /// </summary>
//    /// <remarks>
//    /// Protocol: 
//    ///   TCP_OPEN, 
//    ///   SEND [2 bytes] 0x00: flash program
//    ///   SEND [128Kb of raw data]: follow the whole new BIOS, contains also empty blocks
//    ///   RCV [word:err] (if 0: OK, otherwise error and abort) 
//    ///   SEND [16 bytes]: follow a validity map for ROM erasures (128Kb / 1024 rows = 128 bits). (*)
//    ///   SEND [256 bytes]: follow a validity map for ROM writings (128Kb / 64 blocks = 2048 bits) (*)
//    ///   RCV [word:err] (if 0: OK, otherwise error and abort) 
//    ///   SEND 0x55, 0xAA: to start programming and then reboot
//    ///   
//    ///  (*) Note: the last row of ROM (1Kb) is not erasable nor writable. It contains the flash procs and PIC configuration
//    ///  data.
//    /// </remarks>
//    [SinkId(SinkTypes.RomFlasher)]
//    class RomFlasherSink : Sink
//    {
//        private Timer _timer;

//        public RomFlasherSink()
//        {
//            _timer = new Timer(o => SendProgram(), null, 500, Timeout.Infinite);
//        }

//        public enum MessageType : ushort
//        {
//            FlashMainProgram = 0,
//        }

//        public class MessageBase
//        {
//            [Selector(MessageType.FlashMainProgram, typeof(MainProgramMessage1))]
//            public MessageType Type;
//        }

//        public class MainProgramMessage1 : MessageBase
//        {
//            public MainProgramMessage1()
//            {
//                Type = MessageType.FlashMainProgram;
//            }

//            [SerializeAsFixedArray(128 * 1024)]
//            public byte[] RomData;
//        }

//        public class MainProgramMessage2
//        {
//            [SerializeAsFixedArray(16)]
//            public byte[] ErasureMap;
//            [SerializeAsFixedArray(256)]
//            public byte[] WritingMap;
//        }

//        public class MainProgramMessage3
//        {
//            [SerializeAsFixedArray(2)]
//            public byte[] ControlCode = new byte[] { 0x55, 0xaa };
//        }

//        private void SendProgram()
//        {
//            using (IConnection conn = Open())
//            {
//                MainProgramMessage1 msg1 = new MainProgramMessage1 { RomData = new byte[128 * 1024] };
//                conn.Write(msg1);

//                ValidateResponse(conn);

//                MainProgramMessage2 msg2 = new MainProgramMessage2 { ErasureMap = new byte[16], WritingMap = new byte[256] };
//                conn.Write(msg2);

//                ValidateResponse(conn);

//                MainProgramMessage3 msg3 = new MainProgramMessage3();
//                conn.Write(msg3);
//            }
//        }

//        private void ValidateResponse(IConnection connection)
//        {
//            FlashAckResponse response = connection.Read<FlashAckResponse>();
//            if (response.ErrCode != ErrorCode.Ok)
//            {
//                throw new InvalidOperationException("Error code: " + response.ErrCode);
//            }
//        }
//    }
//}
