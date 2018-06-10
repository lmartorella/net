using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Lucky.Serialization
{
    class ClassSerializer<T> : ISerializer where T : class, new()
    {
        private TypeSerializer[] _fields;

        public ClassSerializer()
        {
            _fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(fi => fi.GetCustomAttribute<NoSerializeAttribute>() == null)
                .OrderBy(fi => fi.MetadataToken) // undocumented, to have the source definition order
                .Select(fi => new TypeSerializer(fi, fi.FieldType)).ToArray();
        }

        public async Task Serialize(Stream writer, object source, object instance)
        {
            foreach (var tuple in _fields)
            {
                object fieldValue = tuple.FieldInfo.GetValue(source);
                await tuple.Serialize(writer, fieldValue, source);
            }
        }

        public async Task<object> Deserialize(Stream reader, object instance)
        {
            T retValue = new T();
            foreach (var tuple in _fields)
            {
                tuple.FieldInfo.SetValue(retValue, await tuple.Deserialize(reader, retValue));
            }
            return retValue;
        }
    }
}
