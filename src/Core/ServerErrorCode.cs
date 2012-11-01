using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucky.Home.Core
{
    enum ServerErrorCode
    {
        Ok = 0,
        UnknownMessage = 1,
        AssignGuid = 2, // follow 16-bytes of guid
        UnknownSinkType = 3,
        UnknownAddress = 4,
    }
}
