namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Some sinks exposes collection of logical sub-sinks
    /// </summary>
    public class SubSink
    {
        /// <summary>
        /// Owner sink
        /// </summary>
        public ISink Sink { get; private set; }

        /// <summary>
        /// Index of the current sub-sink
        /// </summary>
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

        public bool IsOnline
        {
            get
            {
                return Sink.IsOnline;
            }
        }
    }
}