using System;
using System.IO;
using System.Threading;
using Lucky.Home.Core;
using Lucky.Home.Core.Serialization;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Flash a new boot ROM for 18f87j60
    /// </summary>
    /// <remarks>
    /// Protocol: 
    ///   TCP_OPEN, 
    ///   -> [2 bytes] 0x00: flash program
    ///   -> [128Kb of raw data]: follow the whole new BIOS, contains also empty blocks
    ///   <- [word:err] (if 0: OK, otherwise error and abort) 
    ///   -> [16 bytes]: follow a validity map for ROM erasures (128Kb / 1024 rows = 128 bits). (*)
    ///   -> [256 bytes]: follow a validity map for ROM writings (128Kb / 64 blocks = 2048 bits) (*)
    ///   <- [word:err] (if 0: OK, otherwise error and abort) 
    ///   -> 0x55, 0xAA: to start programming and then reboot
    ///   
    ///  (*) Note: the last row of ROM (1Kb) is not erasable nor writable. It contains the flash procs and PIC configuration
    ///  data.
    /// </remarks>
    [DeviceId(DeviceIds.Flasher)]
    class RomFlasherSink : Sink
    {
        private Timer _timer;

        public RomFlasherSink()
        {
            _timer = new Timer(o => SendProgram(), null, 500, Timeout.Infinite);
        }

        private enum MessageType : ushort
        {
            FlashMainProgram = 0,
        }

        private class MessageBase
        {
            [Selector(MessageType.FlashMainProgram, typeof(MainProgramMessage1))]
            public MessageType Type;
        }

        private class MainProgramMessage1 : MessageBase
        {
            public MainProgramMessage1()
            {
                Type = MessageType.FlashMainProgram;
            }

            [SerializeAsFixedArray(128 * 1024)]
            public byte[] RomData;
        }

        private class MainProgramMessage2
        {
            [SerializeAsFixedArray(16)]
            public byte[] ErasureMap;
            [SerializeAsFixedArray(256)]
            public byte[] WritingMap;
        }

        private class MainProgramMessage3
        {
            [SerializeAsFixedArray(2)]
            public byte[] ControlCode = new byte[] { 0x55, 0xaa };
        }

        public void SendProgram()
        {
            using (IConnection conn = Open())
            {
                MainProgramMessage1 msg1 = new MainProgramMessage1 { RomData = new byte[128 * 1024] };
                NetSerializer<MainProgramMessage1>.Write(msg1, conn.Writer);

                ValidateResponse(conn.Reader);

                MainProgramMessage2 msg2 = new MainProgramMessage2 { ErasureMap = new byte[16], WritingMap = new byte[256] };
                NetSerializer<MainProgramMessage2>.Write(msg2, conn.Writer);

                ValidateResponse(conn.Reader);

                MainProgramMessage3 msg3 = new MainProgramMessage3();
                NetSerializer<MainProgramMessage3>.Write(msg3, conn.Writer);
            }
        }

        private void ValidateResponse(BinaryReader reader)
        {
            FlashAckResponse response = NetSerializer<FlashAckResponse>.Read(reader);
            if (response.ErrCode != ErrorCode.Ok)
            {
                throw new InvalidOperationException("Error code: " + response.ErrCode);
            }
        }
    }
}
