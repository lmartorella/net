using Lucky.Home.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Lucky.Home.Serialization
{
    /// <summary>
    /// Serializer for array fields
    /// </summary>
    class ArraySerializer<T> : ISerializer
    {
        private readonly ISerializer _elementSerializer;
        private readonly int _forcedSize;
        private readonly Dictionary<int, DynArrayCaseAttribute> _cases;
        private readonly string _fieldName;

        public ArraySerializer(FieldInfo filedInfo, string fieldName, int forcedSize, Dictionary<int, DynArrayCaseAttribute> cases)
        {
            _fieldName = fieldName;
            _forcedSize = forcedSize;
            _cases = cases;
            _elementSerializer = new TypeSerializer(filedInfo, typeof(T), "(el)" + _fieldName);
        }

        public async Task Serialize(Stream stream, object source, object instance)
        {
            if (_cases != null)
            {
                throw new NotImplementedException("Cases not supported when serializing");
            }

            T[] array = (T[])source;
            int size = _forcedSize;
            if (size == 0)
            {
                size = array.Length;
                // Serialize count as word
                if (await stream.SafeWriteAsync(BitConverter.GetBytes((ushort)size), 2) < 2)
                {
                    // EOF
                    return;
                }
            }
            else if (size < 0)
            {
                throw new ArgumentException("Invalid forced size: " + size);
            }

            // Serialize items
            for (int i = 0; i < size; i++)
            {
                await _elementSerializer.Serialize(stream, array[i], null);
            }
        }

        public async Task<object> Deserialize(Stream reader, object instance)
        {
            int size = _forcedSize;
            if (size <= 0)
            {
                // Read size
                byte[] buffer = new byte[2];
                var b = await reader.SafeReadAsync(buffer, 2);
                if (b < 2)
                {
                    throw new BufferUnderrunException(2, b, "(sizeof)" + (_fieldName ?? ""));
                }
                size = BitConverter.ToInt16(buffer, 0);

                if (size < 0)
                {
                    // Check for cases to transform the error
                    if (_cases != null)
                    {
                        DynArrayCaseAttribute specialCase;
                        if (_cases.TryGetValue(size, out specialCase))
                        {
                            // Treat special case
                            specialCase.FieldInfo.SetValue(instance, specialCase.FieldValue);
                            return new T[0];
                        }
                        else
                        {
                            throw new ArgumentException("Invalid error for dynamic size: " + size);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Dynamic Array with negative size: " + size);
                    }
                }
            }

            T[] array = new T[size];
            for (int i = 0; i < size; i++)
            {
                array[i] = (T)(await _elementSerializer.Deserialize(reader, null));
            }
            return array;
        }
    }
}
