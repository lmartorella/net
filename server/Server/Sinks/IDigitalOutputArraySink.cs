namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Digital outputs array
    /// </summary>
    internal interface IDigitalOutputArraySink : ISink
    {
        /// <summary>
        /// Get/Set the current switch status (get is cached)
        /// </summary>
        bool[] Status { get; set; }
    }
}