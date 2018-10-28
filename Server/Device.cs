using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Device
    {
        internal String MAC;
        internal DateTime firstSeen =DateTime.MinValue;
        internal DateTime lastSeen =DateTime.MinValue;
        internal ArrayList positions = new ArrayList();
        internal HashSet<String> requestedSSIDs = new HashSet<string>();
    }
}
