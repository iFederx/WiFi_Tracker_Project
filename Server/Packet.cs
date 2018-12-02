using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Packet
    {
        internal class Reception
        {
            internal Station ReceivingStation;
            internal double RSSI;
        }
        internal List<Reception> Receivings = new List<Reception>();
        internal String SendingMAC;
        internal String RequestedSSID;//null if no requested SSID
        internal DateTime Timestamp;
        internal String Hash;
    }
}
