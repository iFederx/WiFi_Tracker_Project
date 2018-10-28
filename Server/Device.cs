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
        String MAC;
        DateTime firstSeen=DateTime.MinValue;
        DateTime lastSeen=DateTime.MinValue;
        ArrayList positions = new ArrayList();
        HashSet<String> requestedSSIDs = new HashSet<string>();
    }
}
