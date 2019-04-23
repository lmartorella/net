using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lucky.Home
{
    /// <summary>
    /// Serializable unique ID.
    /// Support GUIDS or ASCII string (16 chars max)
    /// </summary>
    [DataContract]
    internal class NodeId : ISerializable
    {
        private static Regex StrRegex = new Regex("[A-Z0-9_]+");

        [DataMember]
        public Guid Guid;

        [DataMember]
        public string String;

        public bool IsEmpty
        {
            get
            {
                return String == null && Guid == Guid.Empty;
            }
        }

        public NodeId()
        {
            Guid = Guid.Empty;
        }

        public NodeId(Guid guid)
        {
            Guid = guid;
        }

        public NodeId(string str)
        {
            if (str == null || !StrRegex.IsMatch(str) || str.Length > 15)
            {
                throw new ArgumentException("Invalid node Id");
            } 
            String = str;
        }

        public override string ToString()
        {
            return String != null ? String : Guid.ToString();
        }

        public static bool TryParse(string str, out NodeId nodeId)
        {
            Guid guid;
            if (Guid.TryParse(str, out guid))
            {
                nodeId = new NodeId(guid);
                return true;
            }
            else if (StrRegex.IsMatch(str) && str.Length <= 15)
            {
                nodeId = new NodeId(str);
                return true;
            }
            else
            {
                nodeId = null;
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            NodeId other = obj as NodeId;
            if (other == null)
            {
                return false;
            }
            return Guid.Equals(other.Guid) && String == other.String;
        }

        public override int GetHashCode()
        {
            return String != null ? String.GetHashCode() : Guid.GetHashCode();
        }

        byte[] ISerializable.Serialize()
        {
            if (String != null)
            {
                byte[] ret = new byte[16];
                Encoding.ASCII.GetBytes(String, 0, String.Length, ret, 0);
                return ret;
            }
            else
            {
                return Guid.ToByteArray();
            }
        }

        async Task ISerializable.Deserialize(Func<int, Task<byte[]>> feeder)
        {
            byte[] data = await feeder(16);
            // Strip leading zeroes
            int pos = data.ToList().FindLastIndex(b => b != 0);
            string str = Encoding.ASCII.GetString(data, 0, pos + 1);
            if (StrRegex.IsMatch(str))
            {
                String = str;
                Guid = Guid.Empty;
            }
            else
            {
                Guid = new Guid(data);
            }
        }
    }
}
