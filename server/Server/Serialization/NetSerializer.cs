using Lucky.Services;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

// ReSharper disable StaticMemberInGenericType

namespace Lucky.Home.Serialization
{
    interface INetSerializer
    {
        void Serialize(object value, BinaryWriter writer);
        object Deserialize(BinaryReader reader);
    }

    /// <summary>
    /// Class to serialize/deserialize a complex type
    /// </summary>
    class NetSerializer<T> : INetSerializer
    {
        private static readonly INetSerializer s_directSerializer;
        private static readonly ILogger s_logger;

        static NetSerializer()
        {
            s_directSerializer = BuildFieldItem(null, typeof(T));
            s_logger = Manager.GetService<LoggerFactory>().Create("NetSerializer");
        }

        private static INetSerializer BuildFieldItem(FieldInfo fieldInfo, Type fieldType, string subFieldName = "")
        {
            var fieldName = subFieldName + (fieldInfo?.Name ?? "");

            if (fieldType.IsArray)
            {
                var elType = fieldType.GetElementType();
                var elSerializer = BuildFieldItem(fieldInfo, elType, "(el)");

                if (fieldInfo == null || fieldInfo.GetCustomAttributes(typeof(SerializeAsDynArrayAttribute), false).Length < 1)
                {
                    throw new NotSupportedException("Array type not annoted: " + fieldInfo);
                }
                return new ArraySerializer(elSerializer, elType, false, "(arr)" + fieldName, fieldInfo.GetCustomAttributes<DynArrayCaseAttribute>().ToArray());
            }
            
            if (fieldType == typeof(string))
            {
                INetSerializer elSerializer = new AsciiCharSerializer(fieldName);

                SerializeAsFixedStringAttribute fixedAttr = null;
                SerializeAsDynArrayAttribute dyndAttr = null;
                if (fieldInfo != null)
                {
                    fixedAttr = fieldInfo.GetCustomAttributes(typeof(SerializeAsFixedStringAttribute), false).Cast<SerializeAsFixedStringAttribute>().FirstOrDefault();
                    dyndAttr = fieldInfo.GetCustomAttributes(typeof(SerializeAsDynArrayAttribute), false).Cast<SerializeAsDynArrayAttribute>().FirstOrDefault();
                }
                if (fixedAttr != null)
                {
                    return new FixedStringFieldItem(elSerializer, typeof(char), fixedAttr.Size, fieldName);
                }
                else if (dyndAttr != null)
                {
                    return new DynStringFieldItem(elSerializer, typeof(char), fieldName);
                }
                else
                {
                    throw new NotSupportedException("Array type not annoted: " + fieldInfo);
                }
            }
            
            if (fieldType.IsEnum)
            {
                fieldType = fieldType.GetEnumUnderlyingType();
            }

            if (fieldType == typeof(byte))
            {
                return new ByteSerializer(fieldName);
            }
            else if (fieldType == typeof(ushort))
            {
                return new BitConverterItem<ushort>(2, BitConverter.GetBytes, a => BitConverter.ToUInt16(a, 0), fieldName);
            }
            else if (fieldType == typeof(short))
            {
                return new BitConverterItem<short>(2, BitConverter.GetBytes, a => BitConverter.ToInt16(a, 0), fieldName);
            }
            else if (fieldType == typeof(uint))
            {
                return new BitConverterItem<uint>(4, BitConverter.GetBytes, a => BitConverter.ToUInt32(a, 0), fieldName);
            }
            else if (fieldType == typeof(int))
            {
                return new BitConverterItem<int>(4, BitConverter.GetBytes, a => BitConverter.ToInt32(a, 0), fieldName);
            }
            else if (fieldType == typeof(Guid))
            {
                return new GuidFieldItem(fieldName);
            }
            else if (fieldType == typeof(IPAddress))
            {
                return new IpAddressFieldItem(fieldName);
            }
            else if (fieldType.IsClass)
            {
                // Build nested class
                Type serType = typeof(NetSerializer<>.ClassConverter).MakeGenericType(fieldType);
                return (INetSerializer)Activator.CreateInstance(serType);
            }
            throw new NotSupportedException("Type not supported: " + fieldType);
        }

        private class ClassConverter : INetSerializer
        {
            private readonly Tuple<FieldInfo, INetSerializer>[] _fields;

            public ClassConverter()
            {
                _fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .OrderBy(fi => fi.MetadataToken) // undocumented, to have the source definition order
                    .Select(fi => new Tuple<FieldInfo, INetSerializer>(fi, BuildFieldItem(fi, fi.FieldType))).ToArray();
            }

            public void Serialize(object source, BinaryWriter writer)
            {
                foreach (var tuple in _fields)
                {
                    FieldInfo fi = tuple.Item1;
                    INetSerializer ser = tuple.Item2;
                    object fieldValue = fi.GetValue(source);
                    ser.Serialize(fieldValue, writer);
                }
            }

            public object Deserialize(BinaryReader reader)
            {
                T retValue = Activator.CreateInstance<T>();
                foreach (var tuple in _fields)
                {
                    FieldInfo fi = tuple.Item1;
                    INetSerializer ser = tuple.Item2;
                    try
                    {
                        fi.SetValue(retValue, ser.Deserialize(reader));
                    }
                    catch (BufferUnderrunException exc)
                    {
                        throw new BufferUnderrunException(exc, typeof(T));
                    }
                }
                return retValue;
            }
        }

        private class BitConverterItem<TD> : INetSerializer where TD : struct
        {
            private readonly Func<TD, byte[]> _ser;
            private readonly Func<byte[], TD> _deser;
            private readonly string _fieldName;

            private int Size { get; set; }

            public BitConverterItem(int size, Func<TD, byte[]> ser, Func<byte[], TD> deser, string fieldName)
            {
                Size = size;
                _fieldName = fieldName;
                _ser = ser;
                _deser = deser;
            }

            public void Serialize(object source, BinaryWriter writer)
            {
                writer.Write(_ser((TD)source));
            }

            public object Deserialize(BinaryReader reader)
            {
                byte[] b;
                try
                {
                    b = reader.ReadBytes(Size);
                }
                catch (Exception)
                {
                    b = new byte[0];
                }

                if (b.Length < Size)
                {
                    throw new BufferUnderrunException(Size, b, _fieldName);
                }
                return _deser(b);
            }
        }

        private class AsciiCharSerializer : ByteSerializer
        {
            public AsciiCharSerializer(string fieldName)
                :base(fieldName)
            { }

            public override void Serialize(object source, BinaryWriter writer)
            {
                base.Serialize(Encoding.ASCII.GetBytes(new[] { (char)source }, 0, 1)[0], writer);
            }

            public override object Deserialize(BinaryReader reader)
            {
                return Encoding.ASCII.GetChars(new[] { (byte)base.Deserialize(reader) }, 0, 1)[0];
            }
        }

        private class ByteSerializer : INetSerializer
        {
            private readonly string _fieldName;

            public ByteSerializer(string fieldName)
            {
                _fieldName = fieldName;
            }

            public virtual void Serialize(object source, BinaryWriter writer)
            {
                writer.Write((byte)source);
            }

            public virtual object Deserialize(BinaryReader reader)
            {
                try
                {
                    return reader.ReadByte();
                }
                catch (Exception)
                {
                    throw new BufferUnderrunException(1, null, _fieldName);
                }
            }
        }

        private class FixedStringFieldItem : ArraySerializer
        {
            public FixedStringFieldItem(INetSerializer elementSerializer, Type elType, int size, string fieldName)
                :base(elementSerializer, elType, true, fieldName, new DynArrayCaseAttribute[0])
            {
                if (size <= 0)
                {
                    throw new ArgumentException("size");
                }
                ForcedSize = size;
            }
        }

        private class DynStringFieldItem : ArraySerializer
        {
            public DynStringFieldItem(INetSerializer elementSerializer, Type elType, string fieldName)
                : base(elementSerializer, elType, true, fieldName, new DynArrayCaseAttribute[0])
            { }
        }

        private class IpAddressFieldItem : INetSerializer
        {
            private const int Size = 4;
            private readonly string _fieldName;

            public IpAddressFieldItem(string fieldName)
            {
                _fieldName = fieldName;
            }

            public void Serialize(object source, BinaryWriter writer)
            {
                IPAddress address = (IPAddress)source;
                byte[] chars = address.GetAddressBytes();
                writer.Write(chars);
            }

            public object Deserialize(BinaryReader reader)
            {
                byte[] ids;
                try
                {
                    ids = reader.ReadBytes(Size);
                }
                catch
                {
                    ids = new byte[0];
                }
                if (ids.Length < Size)
                {
                    throw new BufferUnderrunException(Size, ids, _fieldName);
                }
                return new IPAddress(ids);
            }
        }

        private class GuidFieldItem : INetSerializer
        {
            private const int Size = 16;
            private readonly string _fieldName;

            public GuidFieldItem(string fieldName)
            {
                _fieldName = fieldName;
            }

            public void Serialize(object source, BinaryWriter writer)
            {
                Guid guid = (Guid)source;
                byte[] chars = guid.ToByteArray();
                writer.Write(chars);
            }

            public object Deserialize(BinaryReader reader)
            {
                byte[] ids;
                try
                {
                    ids = reader.ReadBytes(Size);
                }
                catch
                {
                    ids = new byte[0];
                }
                if (ids.Length < Size)
                {
                    throw new BufferUnderrunException(Size, ids, _fieldName);
                }
                return new Guid(ids);
            }
        }

        private class ArraySerializer : INetSerializer
        {
            private readonly INetSerializer _elementSerializer;
            private readonly Type _elementType;
            protected int ForcedSize { get; set; }
            private readonly bool _isString;
            private readonly string _fieldName;
            private readonly DynArrayCaseAttribute[] _cases;

            public ArraySerializer(INetSerializer elementSerializer, Type elType, bool isString, string fieldName, DynArrayCaseAttribute[] cases)
            {
                _elementSerializer = elementSerializer;
                _elementType = elType;
                _isString = isString;
                _fieldName = fieldName;
                _cases = cases;
                ForcedSize = 0;
            }

            public void Serialize(object source, BinaryWriter writer)
            {
                Array array;
                if (_isString)
                {
                    array = ((string)source).ToCharArray();
                }
                else
                {
                    array = (Array)source;
                }

                int size = ForcedSize;
                if (size <= 0)
                {
                    size = array.Length;
                    // Serialize count as word
                    writer.Write(BitConverter.GetBytes((ushort)size));
                }

                // Serialize items
                for (int i = 0; i < size; i++)
                {
                    _elementSerializer.Serialize(array.GetValue(i), writer);
                }
            }

            public object Deserialize(BinaryReader reader)
            {
                int size = ForcedSize;
                if (size <= 0)
                {
                    // Read size
                    var b = reader.ReadBytes(2);
                    if (b.Length < 2)
                    {
                        throw new BufferUnderrunException(2, b, "(sizeof)" + (_fieldName ?? ""));
                    }
                    size = BitConverter.ToUInt16(b, 0);
                }

                if (_cases.Length > 0)
                {
                    var cm = _cases.FirstOrDefault(c => c.Key == size);
                    if (cm != null)
                    {
                        throw (Exception)Activator.CreateInstance(cm.ExcType);
                    }
                }

                Array array = Array.CreateInstance(_elementType, size);
                for (int i = 0; i < size; i++)
                {
                    array.SetValue(_elementSerializer.Deserialize(reader), i);
                }

                if (_isString)
                {
                    return new string((char[])array);
                }
                else
                {
                    return array;
                }
            }
        }

        public static T Read(BinaryReader reader)
        {
            try
            {
                return (T)s_directSerializer.Deserialize(reader);
            }
            catch (BufferUnderrunException exc)
            {
                s_logger.Error(exc.Message);
                return default(T);
            }
        }

        object INetSerializer.Deserialize(BinaryReader reader)
        {
            return Read(reader);
        }

        public static void Write(T value, BinaryWriter writer)
        {
            // Can be an inherited type...
            Type serType = typeof(NetSerializer<>).MakeGenericType(value.GetType());
            INetSerializer ser = (INetSerializer)Activator.CreateInstance(serType);
            ser.Serialize(value, writer);
        }

        void INetSerializer.Serialize(object source, BinaryWriter writer)
        {
            s_directSerializer.Serialize(source, writer);
        }
    }
}
