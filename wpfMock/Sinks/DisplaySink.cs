//using System;
//using System.IO;
//using System.Text;

//namespace Lucky.HomeMock.Sinks
//{
//    class DisplaySink : SinkBase
//    {
//        private const int StartPort = 18000;

//        public DisplaySink()
//            : base(StartPort, 0, 1)
//        {   }

//        protected override void OnSocketOpened(Stream stream)
//        {
//            using (BinaryReader reader = new BinaryReader(stream))
//            {
//                int l = reader.ReadInt16();
//                string str = ASCIIEncoding.ASCII.GetString(reader.ReadBytes(l));
//                if (Data != null)
//                {
//                    Data(this, new DataEventArgs(str));
//                }
//                using (BinaryWriter writer = new BinaryWriter(stream))
//                {
//                    byte[] errorCode = { 0, 0 };
//                    writer.Write(errorCode);
//                }
//            }
//        }

//        public event EventHandler<DataEventArgs> Data;
//    }
//}
