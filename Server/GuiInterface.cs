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
                return (int)DisplayableType.DeviceDevicePosition | (int)DisplayableType.Stat;
            }
        }

        internal override void publishPosition(Device d, EventType e)
        {
            System.Diagnostics.Debug.Print("DEVICE POSITION: " + d.lastPosition.X + " " + d.lastPosition.Y);
        }

        internal override void publishRename(string oldId, string newId) { throw new NotImplementedException(); }

        internal override void publishSSID(Device d, string SSID) { throw new NotImplementedException(); }

        internal override void publishStat(double stat, PositionTools.Room r, StatType s)
        {
            System.Diagnostics.Debug.Print("ROOM STAT: " + r.roomName + " count: " + stat);
        }
    }
}
