using System;

namespace Lucky.Home.Serialization
{
    /// <summary>
    /// Raised when trying to deserialize a stream closed or with less data than required
    /// </summary>
    public class BufferUnderrunException : Exception
    {
        public BufferUnderrunException(BufferUnderrunException innerExc, Type owner)
            :base(string.Format("{1}: in type {0}", owner.Name, innerExc.Message))
        { }

        public BufferUnderrunException(int requestedSize, int receivedSize, string fieldName)
            :base(string.Format("Buffer underrun in reading {0} bytes for field {1}, received {2}", requestedSize, fieldName ?? "<null>", receivedSize))
        { }
    }
}
