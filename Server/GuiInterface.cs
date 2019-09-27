using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Panopticon
{
    class GuiInterface : Publisher
    {
        internal volatile MainWindow linkedwindow = null;
        internal volatile Room linkedroom = null;

        internal void kill()
        {
            linkedwindow = null;
        }
        internal override int SupportedOperations
        {
            get
            {
                return (int)DisplayableType.DeviceDevicePosition | (int)DisplayableType.AggregatedStat | (int) DisplayableType.RoomUpdate | (int) DisplayableType.StationUpdate;
            }
        }

        internal override void publishPosition(Device d, PositionTools.Position p, EventType e)
        {
            if (linkedwindow != null)
            {
                if (p.room == linkedroom || e == EventType.Disappear || e == EventType.MoveOut)
                {
                    Application.Current.Dispatcher.BeginInvoke((Action)(() => { linkedwindow.updateDevicePosition(d, p, e); }));
                }
            }
            System.Diagnostics.Debug.Print("DEVICE POSITION: " + p.X + " " + p.Y);
        }

        internal override void publishStat(double stat, Room r, DateTime statTime, StatType s)
        {
            if (linkedwindow!=null)
            {
                if (s == StatType.OneSecondDeviceCount)
                    Application.Current.Dispatcher.BeginInvoke((Action)(() => { linkedwindow.updateOneSecondDeviceCount(r, stat); }));
                else
                    Application.Current.Dispatcher.BeginInvoke((Action)(() => { linkedwindow.updateTenMinutesDeviceCount(r, stat); }));
            }
            System.Diagnostics.Debug.Print("GUI ROOM STAT: " + r.roomName + " count: " + stat);
        }
        internal override void publishRoomUpdate(Room r, EventType e)
        {
            System.Diagnostics.Debug.Print("-----------------------GI" + linkedwindow);
            if (linkedwindow != null)
            {
                if (e == EventType.Appear)
                    Application.Current.Dispatcher.BeginInvoke((Action)(() => { linkedwindow.addRoom(r); }));
                else if (e == EventType.Disappear)
                    Application.Current.Dispatcher.BeginInvoke((Action)(() => { linkedwindow.removeRoom(r); }));
            }
            
        }
        internal override void publishStationUpdate(Room r, Station s, EventType e)
        {
            if(linkedwindow!=null)
                Application.Current.Dispatcher.BeginInvoke((Action)(() => { linkedwindow.updateStation(r,s,e); }));
        }

    }
}
