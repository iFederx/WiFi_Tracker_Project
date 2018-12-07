using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    abstract class Publisher
    {
        internal enum EventType { Appear, Update, MoveOut, MoveIn, Disappear };
        internal enum StatType { TenMinutePeopleCount, HalfSecondPeopleCount, InstantaneousPeopleCount };
        internal class Displayable
        {
            internal enum DisplayableType {Device,Stat,Rename};
            internal Displayable(Object o1,Object o2,Object o3,DisplayableType t)
            {
                arg1 = o1;
                arg2 = o2;
                argtype = o3;
                type = t;
            }
            Object arg1;
            Object arg2;
            Object argtype;
            DisplayableType type;
        }
        BlockingCollection<Displayable> todo = new BlockingCollection<Displayable>();
        internal virtual void publishPosition(Device d, EventType e)
        {
            todo.Add(new Displayable(d, null, e, Displayable.DisplayableType.Device));
            System.Diagnostics.Debug.Print("DEVICE POSITION: " + d.lastPosition.X + " " + d.lastPosition.Y);
        }
        internal virtual void publishStat(double stat, PositionTools.Room r, StatType s)
        {
            todo.Add(new Displayable(stat, r, s, Displayable.DisplayableType.Stat));
            System.Diagnostics.Debug.Print("ROOM STAT: " + r.roomName + " count: " + stat);
        }
        internal virtual void publishRename(String oldId, String newId)
        {
            todo.Add(new Displayable(oldId, newId, null, Displayable.DisplayableType.Rename));
        }

    }
}
