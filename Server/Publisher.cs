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
        internal enum DisplayableType: int { DeviceDevicePosition=1, SimpleStat=2,Rename=4,SSID=8,RoomUpdate=16,StationUpdate=32, AggregatedStat=64};
        internal class Displayable
        {
            internal Displayable(Object o1, Object o2, Object o3, DisplayableType t)
            {
                arg1 = o1;
                arg2 = o2;
                argtype = o3;
                type = t;
            }
            internal Object arg1;
            internal Object arg2;
            internal Object argtype;
            internal DisplayableType type;
        }
        internal enum EventType { Appear, Update, MoveOut, MoveIn, Disappear };
        internal enum StatType { TenMinuteAveragePeopleCount, OneSecondPeopleCount, InstantaneousPeopleCount };

        internal abstract int SupportedOperations { get; }
       
        internal bool supportsOperation(DisplayableType dt)
        {
            return ((int)dt & this.SupportedOperations) != 0;
        }
        internal virtual void publishPosition(Device d, EventType e) { throw new NotSupportedException(); }
        internal virtual void publishStat(double stat, PositionTools.Room r, DateTime statTime, StatType s) { throw new NotSupportedException(); }
        internal virtual void publishRename(String oldId, String newId) { throw new NotSupportedException(); }
        internal virtual void publishRoomUpdate(PositionTools.Room r, EventType e) { throw new NotSupportedException(); }
        internal virtual void publishStationUpdate(PositionTools.Room r, EventType e) { throw new NotSupportedException(); }

        internal virtual void publishSSID(Device d, String SSID) { throw new NotSupportedException(); }

    }
}
