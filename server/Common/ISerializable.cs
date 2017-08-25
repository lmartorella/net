using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucky
{
    public interface ISerializable
    {
        byte[] Serialize();

        void Deserialize(byte[] data);

        int DataSize { get; }
    }
}
