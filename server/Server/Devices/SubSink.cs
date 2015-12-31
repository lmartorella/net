using Lucky.Home.Sinks;

namespace Lucky.Home.Devices
{
    internal class SubSink
    {
        public ISink Sink { get; private set; }
        public int SubIndex { get; private set; }

        public SubSink(ISink sink, int subIndex)
        {
            Sink = sink;
            SubIndex = subIndex;
        }

        public override bool Equals(object obj)
        {
            var other = obj as SubSink;
            if (other != null)
            {
                return Sink.Equals(other.Sink) && SubIndex == other.SubIndex;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Sink.GetHashCode() + 7 * SubIndex;
        }
    }

    internal class SubSink<T> where T : ISink
    {
        public T Sink { get; private set; }
        public int SubIndex { get; private set; }

        public SubSink(T sink, int subIndex)
        {
            Sink = sink;
            SubIndex = subIndex;
        }

        public static implicit operator SubSink<T>(SubSink sink)
        {
            return new SubSink<T>((T)sink.Sink, sink.SubIndex);
        }

        public override bool Equals(object obj)
        {
            var other = obj as SubSink<T>;
            if (other != null)
            {
                return Sink.Equals(other.Sink) && SubIndex == other.SubIndex;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Sink.GetHashCode() + 7 * SubIndex;
        }
    }
}