using System;
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

        internal abstract void publishPosition(Device d, EventType e);
        internal abstract void publishStat(double stat, PositionTools.Room r, StatType s);
        internal abstract void publishRename(String oldId, String newId);

    }
}
