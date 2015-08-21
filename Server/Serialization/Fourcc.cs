
using System.Diagnostics;

namespace Lucky.Home.Serialization
{
    public class Fourcc
    {
        public Fourcc()
        { }

        public Fourcc(string code)
        {
            Debug.Assert(code.Length == 4);
            Code = code;
        }

        [SerializeAsFixedString(4)]
        public string Code;
    }
}