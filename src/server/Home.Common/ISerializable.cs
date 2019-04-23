using System;
using System.Threading.Tasks;

namespace Lucky.Home
{
    /// <summary>
    /// Custom stream serialization
    /// </summary>
    public interface ISerializable
    {
        byte[] Serialize();

        /// <summary>
        /// Deserialize asking bundle of bytes at a time
        /// </summary>
        Task Deserialize(Func<int, Task<byte[]>> feeder);
    }
}
