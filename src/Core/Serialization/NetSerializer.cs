﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Reflection;

namespace Lucky.Home.Core.Serialization
{
    interface INetSerializer
    {
        void Serialize(object value, BinaryWriter writer);
        object Deserialize(BinaryReader reader);
    }

    /// <summary>
    /// Class to serialize/deserialize a complex type
    /// </summary>
    class NetSerializer<T> : INetSerializer where T : class, new()
    {
        static readonly Tuple<FieldInfo, INetSerializer>[] s_fields;

        static NetSerializer()
        {
            s_fields = typeof(T)
                        .GetFields(BindingFlags.Public | BindingFlags.Instance)
                        .OrderBy(fi => fi.MetadataToken) // undocumented, to have the source definition order
                        .Select(fi => new Tuple<FieldInfo, INetSerializer>(fi, BuildFieldItem(fi, fi.FieldType))).ToArray();
        }

        private static INetSerializer BuildFieldItem(ICustomAttributeProvider fieldInfo, Type fieldType)
        {
            if (fieldType.IsArray)
            {
                if (fieldInfo.GetCustomAttributes(typeof(SerializeAsDynArrayAttribute), false).Length >= 1)
                {
                    Type elType = fieldType.GetElementType();
                    return new ArraySerializer(BuildFieldItem(fieldInfo, elType), elType);
                }
                else
                {
                    throw new NotSupportedException("Array type not annoted: " + fieldInfo);
                }
            }
            if (fieldType == typeof(string))
            {
                // String!
                SerializeAsCharArrayAttribute attr = fieldInfo.GetCustomAttributes(typeof(SerializeAsCharArrayAttribute), false).Cast<SerializeAsCharArrayAttribute>().FirstOrDefault();
                if (attr == null)
                {
                    throw new NotSupportedException("Missing SerializeAsCharArrayAttribute on string");
                }
                return new FixedAsciiStringFieldItem(attr.Size);
            }
            if (fieldType.IsEnum)
            {
                fieldType = fieldType.GetEnumUnderlyingType();
            }
            if (fieldType == typeof(ushort))
            {
                return new BitConverterItem<ushort>(2, v => BitConverter.GetBytes(v), a => BitConverter.ToUInt16(a, 0));
            }
            if (fieldType == typeof(short))
            {
                return new BitConverterItem<short>(2, v => BitConverter.GetBytes(v), a => BitConverter.ToInt16(a, 0));
            }
            if (fieldType == typeof(uint))
            {
                return new BitConverterItem<uint>(4, v => BitConverter.GetBytes(v), a => BitConverter.ToUInt32(a, 0));
            }
            if (fieldType == typeof(int))
            {
                return new BitConverterItem<int>(4, v => BitConverter.GetBytes(v), a => BitConverter.ToInt32(a, 0));
            }
            if (fieldType == typeof(Guid))
            {
                return new GuidFieldItem();
            }
            if (fieldType == typeof(IPAddress))
            {
                return new IPAddressFieldItem();
            }
            if (fieldType.IsClass)
            {
                // Build nested class
                Type serType = typeof(NetSerializer<>).MakeGenericType(fieldType);
                return (INetSerializer)Activator.CreateInstance(serType);
            }
            throw new NotSupportedException("Type not supported: " + fieldType);
        }

        private class BitConverterItem<TD> : INetSerializer where TD : struct
        {
            private Func<TD, byte[]> _ser;
            private Func<byte[], TD> _deser;
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

        private class FixedAsciiStringFieldItem : INetSerializer
        {
            private int Size { get; set; }

            public FixedAsciiStringFieldItem(int size)
            {
                Size = size;
            }

            public void Serialize(object source, BinaryWriter writer)
            {
                string str = (string)source;
                byte[] chars = ASCIIEncoding.ASCII.GetBytes(str.ToCharArray(), 0, Size);
                writer.Write(chars);
            }

            public object Deserialize(BinaryReader reader)
            {
                byte[] b = reader.ReadBytes(Size);
                return ASCIIEncoding.ASCII.GetString(b);
            }
        }

        private class IPAddressFieldItem : INetSerializer
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
            private INetSerializer _elementSerializer;
            private Type _elementType;

            public ArraySerializer(INetSerializer elementSerializer, Type elType)
            {
                _elementSerializer = elementSerializer;
                _elementType = elType;
            }

            public void Serialize(object source, BinaryWriter writer)
            {
                Array array = (Array)source;
                // Serialize count as word
                writer.Write(BitConverter.GetBytes((ushort)array.Length));
                // Serialize items
                foreach (object item in array)
                {
                    _elementSerializer.Serialize(item, writer);
                }
            }

            public object Deserialize(BinaryReader reader)
            {
                // Read size
                int size = BitConverter.ToUInt16(reader.ReadBytes(2), 0);
                Array array = Array.CreateInstance(_elementType, size);
                for (int i = 0; i < size; i++)
                {
                    array.SetValue(_elementSerializer.Deserialize(reader), i);
                }
                return array;
            }
        }

        public static T Read(BinaryReader reader)
        {
            T retValue = new T();
            foreach (var tuple in s_fields)
            {
                FieldInfo fi = tuple.Item1;
                INetSerializer ser = tuple.Item2;
                fi.SetValue(retValue, ser.Deserialize(reader));
            }
            return retValue;
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
            foreach (var tuple in s_fields)
            {
                FieldInfo fi = tuple.Item1;
                INetSerializer ser = tuple.Item2;
                object fieldValue = fi.GetValue(source);
                ser.Serialize(fieldValue, writer);
            }
        }
    }
}
