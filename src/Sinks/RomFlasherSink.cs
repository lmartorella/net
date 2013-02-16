using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucky.Home.Core;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Flash a new boot ROM for 18f87j60
    /// </summary>
    /// <remarks>
    /// Protocol: 
    ///   TCP_OPEN, 
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
        public void SendProgram(Program program)
        {
            using (IConnection conn = Open())
            {
                conn.Writer.Write(program.AllData);
                ushort ack = BitConverter.ToUInt16(conn.Reader.ReadBytes(2), 0);
            }
        }
    }

    class Program
    {
        public byte[] AllData = new byte[128 * 1024];
    }
}
