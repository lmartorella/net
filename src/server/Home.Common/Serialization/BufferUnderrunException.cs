using System;

namespace Lucky.Serialization
{
    public class BufferUnderrunException : Exception
    {
        public BufferUnderrunException(BufferUnderrunException innerExc, Type owner)
            :base(string.Format("{1}: in type {0}", owner.Name, innerExc.Message))
        { }

        public BufferUnderrunException(int requestedSize, string fieldName)
            :base(string.Format("Buffer underrun in reading {0} bytes for field {1}", requestedSize, fieldName ?? "<null>"))
        { }
    }
}
