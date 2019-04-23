
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Lucky.Serialization
{
    [DataContract]
    public class Fourcc
    {
        public Fourcc()
        { }

        public Fourcc(string code)
        {
            Debug.Assert(code.Length == 4);
            Code = code;
        }

        [DataMember]
        [SerializeAsFixedString(4)]
        public string Code;

        public override bool Equals(object obj)
        {
            return Code == ((Fourcc)obj).Code;
        }

        public override int GetHashCode()
        {
            return Code.GetHashCode();
        }
    }
}