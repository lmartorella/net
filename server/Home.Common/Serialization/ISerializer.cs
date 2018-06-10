using System.IO;
using System.Threading.Tasks;

namespace Lucky.Serialization
{
    interface ISerializer
    {
        Task Serialize(Stream stream, object source, object instance);
        Task<object> Deserialize(Stream reader, object instance);
    }
}
