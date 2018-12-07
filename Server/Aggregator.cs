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

        internal override void publishPosition(Device d, EventType e)
        {
            //TODO
            System.Diagnostics.Debug.Print("DEVICE POSITION: "+d.lastPosition.X+" "+d.lastPosition.Y);
        }

        internal override void publishRename(string oldId, string newId)
        {
            //TODO
        }

        internal override void publishStat(double stat, PositionTools.Room r, StatType s)
        {
            //TODO
            System.Diagnostics.Debug.Print("ROOM STAT: "+r.roomName + " count: " + stat);
        }
    }
}
