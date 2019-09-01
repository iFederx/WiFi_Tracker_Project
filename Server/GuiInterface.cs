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
		public static MainWindow statlinkedwindow; //TODO: alternative?

        internal override int SupportedOperations
        {
            get
            {
                return (int)DisplayableType.DeviceDevicePosition | (int)DisplayableType.AggregatedStat | (int) DisplayableType.RoomUpdate | (int) DisplayableType.StationUpdate;
            }
        }

        internal override void publishPosition(Device d, EventType e)
        {
            if(d.lastPosition.room == linkedroom || e == EventType.Disappear || e == EventType.MoveOut)
            {
                Application.Current.Dispatcher.Invoke(() => { linkedwindow.updateDevicePosition(d, d.lastPosition); });
            }
            System.Diagnostics.Debug.Print("DEVICE POSITION: " + d.lastPosition.X + " " + d.lastPosition.Y);
        }

        internal override void publishStat(double stat, Room r, DateTime statTime, StatType s)
        {
            if(s==StatType.OneSecondDeviceCount)
                Application.Current.Dispatcher.Invoke(() => { linkedwindow.updateOneSecondDeviceCount(r,stat); });
            else
                Application.Current.Dispatcher.Invoke(() => { linkedwindow.updateTenMinutesDeviceCount(r, stat); });

            System.Diagnostics.Debug.Print("GUI ROOM STAT: " + r.roomName + " count: " + stat);
        }
        internal override void publishRoomUpdate(Room r, EventType e)
        {
            System.Diagnostics.Debug.Print("-----------------------GI" + linkedwindow);
            if(e == EventType.Appear)
                Application.Current.Dispatcher.Invoke(() => { linkedwindow.addRoom(r); });
            else if(e == EventType.Disappear)
                Application.Current.Dispatcher.Invoke(() => { linkedwindow.removeRoom(r); });
        }
        internal override void publishStationUpdate(Room r, Station s, EventType e)
        {
            Application.Current.Dispatcher.Invoke(() => { linkedwindow.updateStation(r,s,e); });
        }

    }
}
