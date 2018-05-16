namespace Lucky.Home.Sinks
{
    [SinkId("BM18")]
    class BarometricSink : SinkBase
    {
        public byte[] ReadCalibrationData()
        {
            Write(writer =>
            {
                writer.WriteBytes(new byte[] { 22 });
            });
            byte[] data = null;
            Read(reader =>
            {
                data = reader.ReadBytes(22);
            });
            return data;
        }

        public byte[] ReadUncompensatedData()
        {
            Write(writer =>
            {
                writer.WriteBytes(new byte[] { 5 });
            });
            byte[] data = null;
            Read(reader =>
            {
                data = reader.ReadBytes(5);
            });
            return data;
        }
    }
}
