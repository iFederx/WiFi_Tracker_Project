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
        internal enum DisplayableType: int { DeviceDevicePosition=1, Stat=2,Rename=4,SSID=8};
        internal enum EventType { Appear, Update, MoveOut, MoveIn, Disappear };
        internal enum StatType { TenMinutePeopleCount, HalfSecondPeopleCount, InstantaneousPeopleCount };

        internal abstract int SupportedOperations { get; }
       
        internal bool supportsOperation(DisplayableType dt)
        {
            return ((int)dt & this.SupportedOperations) != 0;
        }
        internal abstract void publishPosition(Device d, EventType e);
        internal abstract void publishStat(double stat, PositionTools.Room r, StatType s);
        internal abstract void publishRename(String oldId, String newId);

        internal abstract void publishSSID(Device d, String SSID);

    }
}
