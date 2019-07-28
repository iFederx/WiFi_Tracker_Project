using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Panopticon
{
    class DatabasePublisher:Publisher
    {
        DatabaseInterface DBInt;
        volatile CancellationTokenSource t = new CancellationTokenSource();
        volatile Boolean killed = false;
        public DatabasePublisher(DatabaseInterface di)
        {
            DBInt = di;
        }

        internal override int SupportedOperations
        {
            get
            {
                return (int)DisplayableType.DeviceDevicePosition | (int)DisplayableType.Rename | (int)DisplayableType.SSID | (int)DisplayableType.AggregatedStat;
            }
        }
        BlockingCollection<Displayable> todo = new BlockingCollection<Displayable>();
        internal override void publishPosition(Device d, EventType e)
        {
            if(e!=EventType.Disappear)
                todo.Add(new Displayable(d, null, e, DisplayableType.DeviceDevicePosition));
            System.Diagnostics.Debug.Print("DEVICE POSITION: " + d.lastPosition.X + " " + d.lastPosition.Y);
        }
        internal override void publishStat(double stat, Room r, DateTime statTime, StatType s)
        {
            todo.Add(new Displayable(stat, r, s, DisplayableType.AggregatedStat));
            if (s==StatType.OneSecondDeviceCount)
                System.Diagnostics.Debug.Print("DB ROOM 1sec STAT: " + r.roomName + " count: " + stat);
            else
                System.Diagnostics.Debug.Print("DB ROOM avg STAT: " + r.roomName + " count: " + stat);
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
            Displayable item;
            while (!killed)
            {
                try
                {
                    item = todo.Take(t.Token);
                }
                catch (OperationCanceledException)
                {
                    continue;
                }
                switch (item.type)
                {
                    case DisplayableType.AggregatedStat:
                        {
                            if ((StatType)item.argtype == StatType.OneSecondDeviceCount)
                                DBInt.updateRoomCount((double)item.arg1, ((Room)item.arg2).roomName);
                            else
                                DBInt.addLTRoomCount((double)item.arg1, ((Room)item.arg2).roomName, DateTime.Now);
                            break;
                        }
                    case DisplayableType.SSID:
                        {
                            DBInt.addRequestedSSID(((Device)item.arg1).identifier, (String)item.arg2);
                            break;
                        }
                    case DisplayableType.Rename:
                        {
                            DBInt.renameDevice((String)item.arg1, (String)item.arg2);
                            break;
                        }
                    case DisplayableType.DeviceDevicePosition:
                        {
                            Device d = (Device)item.arg1;
                            DBInt.addDevicePosition(d.identifier,d.MAC,d.lastPosition.room.roomName,d.lastPosition.X,d.lastPosition.Y,d.lastPosition.positionDate);
                            break;
                        }
                }
            }
        }

        internal void kill()
        {
            killed = true;
            t.Cancel();
        }

        
    }
}
