using System.IO;
using System.Threading.Tasks;

namespace Lucky.Home.Serialization
{
    /// <summary>
    /// Generic interface for serializers
    /// </summary>
    interface ISerializer
    {
        Task Serialize(Stream stream, object source, object instance);
        Task<object> Deserialize(Stream reader, object instance);
    }
}
