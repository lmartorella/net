﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Reflection;
// ReSharper disable StaticMemberInGenericType

namespace Lucky.Home.Core
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

        static NetSerializer()
        {
            s_directSerializer = BuildFieldItem(null, typeof(T));
        }

        private static INetSerializer BuildFieldItem(ICustomAttributeProvider fieldInfo, Type fieldType)
        {
            if (fieldType.IsArray)
            {
                var elType = fieldType.GetElementType();
                var elSerializer = BuildFieldItem(fieldInfo, elType);

                if (fieldInfo == null || fieldInfo.GetCustomAttributes(typeof(SerializeAsDynArrayAttribute), false).Length < 1)
                {
                    throw new NotSupportedException("Array type not annoted: " + fieldInfo);
                }
                return new ArraySerializer(elSerializer, elType, false);
            }
            
            if (fieldType == typeof(string))
            {
                var elType = typeof(char);
                INetSerializer elSerializer = new AsciiCharSerializer();

                SerializeAsFixedStringAttribute attr = null;
                if (fieldInfo != null)
                {
                    attr = fieldInfo.GetCustomAttributes(typeof(SerializeAsFixedStringAttribute), false).Cast<SerializeAsFixedStringAttribute>().FirstOrDefault();
                }
                if (attr == null)
                {
                    throw new NotSupportedException("Array type not annoted: " + fieldInfo);
                }
                return new FixedStringFieldItem(elSerializer, elType, attr.Size);
            }
            
            if (fieldType.IsEnum)
            {
                fieldType = fieldType.GetEnumUnderlyingType();
            }

            else if (fieldType == typeof(byte))
            {
                return new ByteSerializer();
            }
            else if (fieldType == typeof(ushort))
            {
                return new BitConverterItem<ushort>(2, BitConverter.GetBytes, a => BitConverter.ToUInt16(a, 0));
            }
            else if (fieldType == typeof(short))
            {
                return new BitConverterItem<short>(2, BitConverter.GetBytes, a => BitConverter.ToInt16(a, 0));
            }
            else if (fieldType == typeof(uint))
            {
                return new BitConverterItem<uint>(4, BitConverter.GetBytes, a => BitConverter.ToUInt32(a, 0));
            }
            else if (fieldType == typeof(int))
            {
                return new BitConverterItem<int>(4, BitConverter.GetBytes, a => BitConverter.ToInt32(a, 0));
            }
            else if (fieldType == typeof(Guid))
            {
                return new GuidFieldItem();
            }
            else if (fieldType == typeof(IPAddress))
            {
                return new IpAddressFieldItem();
            }
            else if (fieldType.IsClass)
            {
                // Build nested class
                Type serType = typeof(ClassConverter);
                return (INetSerializer)Activator.CreateInstance(serType);
            }
            throw new NotSupportedException("Type not supported: " + fieldType);
        }

        private class ClassConverter : INetSerializer
        {
            private readonly Tuple<FieldInfo, INetSerializer>[] _fields;
            //private readonly FieldInfo _selector;

            public ClassConverter()
            {
                _fields = typeof(T)
                    .GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .OrderBy(fi => fi.MetadataToken) // undocumented, to have the source definition order
                    .Select(fi => new Tuple<FieldInfo, INetSerializer>(fi, BuildFieldItem(fi, fi.FieldType))).ToArray();
                //var selectors = _fields.Select(t => t.Item1)
                //    .Where(fi => fi.DeclaringType == typeof(T))
                //    .Where(fi => fi.GetCustomAttributes(typeof(SelectorAttribute), false).Length > 0)
                //    .ToArray();
                //if (selectors.Count() > 1)
                //{
                //    throw new NotSupportedException("Too many selectors on type " + typeof(T));
                //}
                //_selector = selectors.FirstOrDefault();
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
                    fi.SetValue(retValue, ser.Deserialize(reader));
                }
                //if (_selector != null)
                //{
                //    retValue = ProcessSelector(retValue, reader);
                //}
                return retValue;
            }

            //private T ProcessSelector(T value, BinaryReader reader)
            //{
            //    object currValue = _selector.GetValue(value);
            //    SelectorAttribute[] attrs = (SelectorAttribute[])_selector.GetCustomAttributes(typeof(SelectorAttribute), false);
            //    foreach (var attr in attrs)
            //    {
            //        if (attr.SelectorValue.Equals(currValue))
            //        {
            //            Type newType = attr.Type;
            //            if (newType.BaseType != typeof(T))
            //            {
            //                throw new NotSupportedException("Type not directly assignable in selector");
            //            }

            //            // Create new NetSerializer
            //            IPartialSerializer deser = (IPartialSerializer)Activator.CreateInstance(typeof(NetSerializer<>).MakeGenericType(newType));
            //            value = (T)deser.Deserialize(value, _fields.Length, reader);
            //        }
            //    }
            //    return value;
            //}
        }

        private class BitConverterItem<TD> : INetSerializer where TD : struct
        {
            private readonly Func<TD, byte[]> _ser;
            private readonly Func<byte[], TD> _deser;
            private int Size { get; set; }

            public BitConverterItem(int size, Func<TD, byte[]> ser, Func<byte[], TD> deser)
            {
                Size = size;
                _ser = ser;
                _deser = deser;
            }

            public void Serialize(object source, BinaryWriter writer)
            {
                writer.Write(_ser((TD)source));
            }

            public object Deserialize(BinaryReader reader)
            {
                byte[] b = reader.ReadBytes(Size);
                return _deser(b);
            }
        }

        private class AsciiCharSerializer : INetSerializer
        {
            public void Serialize(object source, BinaryWriter writer)
            {
                writer.Write(Encoding.ASCII.GetBytes(new[] { (char)source }, 0, 1));
            }

            public object Deserialize(BinaryReader reader)
            {
                return Encoding.ASCII.GetChars(new[] { reader.ReadByte() }, 0, 1)[0];
            }
        }

        private class ByteSerializer : INetSerializer
        {
            public void Serialize(object source, BinaryWriter writer)
            {
                writer.Write((byte)source);
            }

            public object Deserialize(BinaryReader reader)
            {
                return reader.ReadByte();
            }
        }

        private class FixedStringFieldItem : ArraySerializer
        {
            public FixedStringFieldItem(INetSerializer elementSerializer, Type elType, int size)
                :base(elementSerializer, elType, true)
            {
                if (size <= 0)
                {
                    throw new ArgumentException("size");
                }
                ForcedSize = size;
            }
        }

        private class IpAddressFieldItem : INetSerializer
        {
            private const int Size = 4;

            public void Serialize(object source, BinaryWriter writer)
            {
                IPAddress address = (IPAddress)source;
                byte[] chars = address.GetAddressBytes();
                writer.Write(chars);
            }

            public object Deserialize(BinaryReader reader)
            {
                byte[] ids = reader.ReadBytes(Size);
                return new IPAddress(ids);
            }
        }

        private class GuidFieldItem : INetSerializer
        {
            private const int Size = 16;

            public void Serialize(object source, BinaryWriter writer)
            {
                Guid guid = (Guid)source;
                byte[] chars = guid.ToByteArray();
                writer.Write(chars);
            }

            public object Deserialize(BinaryReader reader)
            {
                byte[] ids = reader.ReadBytes(Size);
                return new Guid(ids);
            }
        }

        private class ArraySerializer : INetSerializer
        {
            private readonly INetSerializer _elementSerializer;
            private readonly Type _elementType;
            protected int ForcedSize { get; set; }
            private readonly bool _isString;

            public ArraySerializer(INetSerializer elementSerializer, Type elType, bool isString)
            {
                _elementSerializer = elementSerializer;
                _elementType = elType;
                _isString = isString;
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
                    size = BitConverter.ToUInt16(reader.ReadBytes(2), 0);
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
            catch (EndOfStreamException)
            {
                return default(T);
            }
        }

        //object IPartialSerializer.Deserialize(object firstPart, int fields, BinaryReader reader)
        //{
        //    // Copy all field values of the base type there
        //    T newValue = default(T);
        //    if (typeof(T).IsClass)
        //    {
        //        newValue = Activator.CreateInstance<T>();
        //    }

        //    foreach (var fieldInfo in s_fields.Take(fields).Select(t => t.Item1))
        //    {
        //        object v = fieldInfo.GetValue(firstPart);
        //        fieldInfo.SetValue(newValue, v);
        //    }

        //    // Now go on with deserialization
        //    Read(ref newValue, reader, fields);
        //    return newValue;
        //}

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
