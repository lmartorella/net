using System.Diagnostics;
using System.Runtime.Serialization;

namespace Lucky.Serialization
{
    [DataContract]
    public class Twocc
    {
        public Twocc()
        { }

        public Twocc(string code)
        {
            Debug.Assert(code.Length == 2);
            Code = code;
        }

        [DataMember]
        [SerializeAsFixedString(2)]
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