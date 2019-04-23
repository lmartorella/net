using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Lucky.Serialization
{
    class TypeSerializer : ISerializer
    {
        private ISerializer _serializer;
        public FieldInfo FieldInfo { get; private set; }

        private int GetForcedSize<TFixed, TDyn>(out Dictionary<int, DynArrayCaseAttribute> cases) where TDyn : Attribute where TFixed : Attribute, IFixedArrayAttribute
        {
            if (FieldInfo == null)
            {
                cases = null;
                return 0;
            }

            cases = FieldInfo.GetCustomAttributes<DynArrayCaseAttribute>().Select(attr =>
            {
                attr.FieldInfo = FieldInfo.DeclaringType.GetField(attr.FieldName);
                return attr;
            }).ToDictionary(a => a.SizeCase);
            if (cases.Keys.Count == 0)
            {
                cases = null;
            }

            TFixed fixedAttr = null;
            TDyn dyndAttr = null;
            if (FieldInfo != null)
            {
                fixedAttr = FieldInfo.GetCustomAttribute<TFixed>(false);
                dyndAttr = FieldInfo.GetCustomAttribute<TDyn>(false);
            }
            if (fixedAttr != null)
            {
                if (dyndAttr != null)
                {
                    throw new NotSupportedException("Array type annoted twice " + FieldInfo);
                }
                return fixedAttr.Size;
            }
            else if (dyndAttr != null)
            {
                return 0;
            }
            else
            {
                throw new NotSupportedException("Array type not annoted: " + FieldInfo);
            }
        }

        public TypeSerializer(FieldInfo fieldInfo, Type fieldType, string subFieldName = "")
        {
            FieldInfo = fieldInfo;
            var fieldName = subFieldName + (fieldInfo?.Name ?? "");

            if (fieldType.IsArray)
            {
                var elType = fieldType.GetElementType();
                Type serType = typeof(ArraySerializer<>).MakeGenericType(elType);

                Dictionary<int, DynArrayCaseAttribute> cases;
                int forcedSize = GetForcedSize<SerializeAsFixedArrayAttribute, SerializeAsDynArrayAttribute>(out cases);
                _serializer = (ISerializer)Activator.CreateInstance(serType, fieldInfo, "(arr)" + fieldName, forcedSize, cases);
            }
            else if (fieldType == typeof(string))
            {
                Dictionary<int, DynArrayCaseAttribute> cases;
                _serializer = new StringSerializer(fieldName, GetForcedSize<SerializeAsFixedStringAttribute, SerializeAsDynStringAttribute>(out cases));
                if (cases != null)
                {
                    throw new NotImplementedException("Cases not implemented for strings");
                }
            }
            else 
            {
                if (fieldType.IsEnum)
                {
                    fieldType = fieldType.GetEnumUnderlyingType();
                }

                if (fieldType == typeof(byte))
                {
                    _serializer = new BitConverterSerializer<byte>(1, a => new byte[1] { a }, a => a[0], fieldName);
                }
                else if (fieldType == typeof(ushort))
                {
                    _serializer = new BitConverterSerializer<ushort>(2, BitConverter.GetBytes, a => BitConverter.ToUInt16(a, 0), fieldName);
                }
                else if (fieldType == typeof(short))
                {
                    _serializer = new BitConverterSerializer<short>(2, BitConverter.GetBytes, a => BitConverter.ToInt16(a, 0), fieldName);
                }
                else if (fieldType == typeof(uint))
                {
                    _serializer = new BitConverterSerializer<uint>(4, BitConverter.GetBytes, a => BitConverter.ToUInt32(a, 0), fieldName);
                }
                else if (fieldType == typeof(int))
                {
                    _serializer = new BitConverterSerializer<int>(4, BitConverter.GetBytes, a => BitConverter.ToInt32(a, 0), fieldName);
                }
                else if (fieldType == typeof(Guid))
                {
                    _serializer = new GuidSerializer(fieldName);
                }
                else if (fieldType == typeof(IPAddress))
                {
                    _serializer = new IpAddressSerializer(fieldName);
                }
                else if (typeof(ISerializable).IsAssignableFrom(fieldType))
                {
                    Type serType = typeof(SerializableItemSerializer<>).MakeGenericType(fieldType);
                    _serializer = (ISerializer)Activator.CreateInstance(serType, fieldName);
                }
                else if (fieldType.IsClass)
                {
                    // Build nested class
                    Type serType = typeof(ClassSerializer<>).MakeGenericType(fieldType);
                    _serializer = (ISerializer)Activator.CreateInstance(serType);
                }
                else
                {
                    throw new NotSupportedException("Type not supported: " + fieldType);
                }
            }
        }

        public Task<object> Deserialize(Stream reader, object instance)
        {
            return _serializer.Deserialize(reader, instance);
        }

        public Task Serialize(Stream stream, object source, object instance)
        {
            return _serializer.Serialize(stream, source, instance);
        }
    }
}
