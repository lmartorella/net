using System.Threading.Tasks;

namespace Lucky.Home.Protocol
{
    /// <summary>
    /// A read-only message-based async stream.
    /// </summary>
    public interface IConnectionReader
    {
        /// <summary>
        /// Read a serializable message from the stream
        /// </summary>
        Task<T> Read<T>();
    }
}