//using System;
//using System.IO;

//namespace Lucky.HomeMock.Sinks
//{
//    class FlasherSink : SinkBase
//    {
//        private const int StartPort = 18010;

//        public FlasherSink()
//            : base(StartPort, 0, 2)
//        { }

//        protected override void OnSocketOpened(Stream stream)
//        {
//            using (BinaryReader reader = new BinaryReader(stream))
//            {
//                // Read type
//                ushort type = reader.ReadUInt16();
//                if (type != 0)
//                {
//                    throw new NotSupportedException();
//                }

//                // Read 128k of data
//                byte[] program = reader.ReadBytes(128 * 1024);

//                // Send OK
//                using (BinaryWriter writer = new BinaryWriter(stream))
//                {
//                    writer.Write((ushort)0);

//                    // Read 16 byte of erasure + 256 of validity
//                    byte[] erasure = reader.ReadBytes(16 + 256);

//                    writer.Write((ushort)0);

//                    // Read program code
//                    ushort code = reader.ReadUInt16();
//                    if (code != 0xaa55)
//                    {
//                        throw new NotSupportedException();
//                    }
//                }
//            }
//        }
//    }
//}
