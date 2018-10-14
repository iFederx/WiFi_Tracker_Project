using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Packet
    {
        public class Reception
        {
            Station ReceivingStation;
            double RSSI;
        }
        List<Reception> Receivings = new List<Reception>();
        String SendingMAC;
        String RequestedSSID;
        DateTime Timestamp;
        String Hash;
    }
}
