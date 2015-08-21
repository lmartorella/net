namespace Lucky.Home.Sinks
{
    public interface IDisplaySink : ISink
    {
        /// <summary>
        /// Immediately write a text line to the display
        /// </summary>
        void Write(string line);

        /// <summary>
        /// How many lines?
        /// </summary>
        int LineCount { get; }

        /// <summary>
        /// How many columns?
        /// </summary>
        int CharCount { get; }
    }
}