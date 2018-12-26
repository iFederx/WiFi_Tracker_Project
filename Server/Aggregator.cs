using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Aggregator : Publisher
    {
        List<Publisher> propagate;
        public Aggregator(List<Publisher> p)
        {
            propagate = new List<Publisher>();
            propagate.AddRange(p);
        }
        internal override int SupportedOperations
        {
            get
            {
                return (int)DisplayableType.Stat;
            }
        }

        internal override void publishPosition(Device d, EventType e) { throw new NotImplementedException(); }

        internal override void publishRename(string oldId, string newId) { throw new NotImplementedException(); }
     
        internal override void publishSSID(Device d, string SSID) { throw new NotImplementedException(); }

        internal override void publishStat(double stat, PositionTools.Room r, StatType s) 
        {
            System.Diagnostics.Debug.Print("ROOM STAT: " + r.roomName + " count: " + stat);
        }
    }
}
