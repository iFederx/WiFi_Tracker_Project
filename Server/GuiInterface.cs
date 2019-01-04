using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class GuiInterface : Publisher
    {
        internal override int SupportedOperations
        {
            get
            {
                return (int)DisplayableType.DeviceDevicePosition | (int)DisplayableType.AggregatedStat | (int) DisplayableType.RoomUpdate | (int) DisplayableType.StationUpdate;
            }
        }

        internal override void publishPosition(Device d, EventType e)
        {
            System.Diagnostics.Debug.Print("DEVICE POSITION: " + d.lastPosition.X + " " + d.lastPosition.Y);
        }

        internal override void publishStat(double stat, PositionTools.Room r, DateTime statTime, StatType s)
        {
            System.Diagnostics.Debug.Print("GUI ROOM STAT: " + r.roomName + " count: " + stat);
        }
        internal override void publishRoomUpdate(PositionTools.Room r, EventType e)
        {
        }
        internal override void publishStationUpdate(PositionTools.Room r, EventType e)
        {
        }

    }
}
