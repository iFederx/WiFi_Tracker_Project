using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class DatabasePublisher:Publisher
    {
        internal class Displayable
        {
            internal Displayable(Object o1, Object o2, Object o3, DisplayableType t)
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
        public DatabasePublisher()
        {
            //TODO
        }

        internal override int SupportedOperations
        {
            get
            {
                return (int)DisplayableType.DeviceDevicePosition | (int)DisplayableType.Rename | (int)DisplayableType.SSID | (int)DisplayableType.Stat;
            }
        }
        BlockingCollection<Displayable> todo = new BlockingCollection<Displayable>();
        internal override void publishPosition(Device d, EventType e)
        {
            todo.Add(new Displayable(d, null, e, DisplayableType.DeviceDevicePosition));
            System.Diagnostics.Debug.Print("DEVICE POSITION: " + d.lastPosition.X + " " + d.lastPosition.Y);
        }
        internal override void publishStat(double stat, PositionTools.Room r, StatType s)
        {
            todo.Add(new Displayable(stat, r, s, DisplayableType.Stat));
            System.Diagnostics.Debug.Print("ROOM STAT: " + r.roomName + " count: " + stat);
        }
        internal override void publishRename(String oldId, String newId)
        {
            todo.Add(new Displayable(oldId, newId, null, DisplayableType.Rename));
            System.Diagnostics.Debug.Print("RENAME: " + oldId+" -> "+newId);
        }

        internal override void publishSSID(Device d, String SSID)
        {
            todo.Add(new Displayable(d, SSID, null, DisplayableType.SSID));
            System.Diagnostics.Debug.Print("REQUESTED SSID: " + SSID);
        }
        internal void databaseProcess()
        {
            //TODO
        }

        internal void kill()
        {
            //TODO
        }

        
    }
}
