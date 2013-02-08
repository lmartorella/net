using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucky.Home.Sinks
{
    static class DeviceIds
    {
        /// <summary>
        /// Simple display protocol
        /// </summary>
        /// <remarks>
        /// Protocol: TCP_OPEN, 
        ///          -> [word:lenght] + ASCII
        ///           TCP_CLOSE
        /// </remarks>
        public const int Display = 1;
        
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
        public const int Flasher = 2;
    }
}
