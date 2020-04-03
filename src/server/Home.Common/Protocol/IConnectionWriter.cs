using System.Threading.Tasks;

namespace Lucky.Home.Protocol
{
    /// <summary>
    /// A write-only message-based async stream.
    /// </summary>
    public interface IConnectionWriter
    {
        /// <summary>
        /// Write a serializable message to the stream
        /// </summary>
        Task Write<T>(T data);
    }
}