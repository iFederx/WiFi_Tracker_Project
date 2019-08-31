using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panopticon
{
    public class Room
    {
        internal static Room externRoom = new Room("External", 0, 0);
        internal String roomName;
        internal Double xlength; //cm
        internal Double ylength; //cm
        internal int stationcount;
        internal int devicecount;
        private HashSet<Device> devicesInRoom = new HashSet<Device>();
        private HashSet<Station> stationsInRoom = new HashSet<Station>();
        private Object slocker = new Object();
        private Object dlocker = new Object();

        public Room(String room_Name, Double x_length, Double y_length)
        {
            this.roomName = room_Name;
            this.xlength = x_length;
            this.ylength = y_length;
        }
        public override bool Equals(object obj)
        {
            Room other = (Room)obj;
            return other.roomName == this.roomName;
        }

        public override int GetHashCode()
        {
            return roomName.GetHashCode();
        }

        public void addStation(Station s)
        {
            lock(slocker)
            {
                stationcount++;
                stationsInRoom.Add(s);
            }
        }
        public void removeStation(Station s)
        {
            lock(slocker)
            {
                stationcount--;
                stationsInRoom.Remove(s);
            }

        }
        public void addDevice(Device d)
        {
            lock(dlocker)
            {
                devicecount++;
                devicesInRoom.Add(d);
            }
        }
        public void removeDevice(Device d)
        {
            lock(dlocker)
            {
                devicecount--;
                devicesInRoom.Remove(d);
            }
        }
        public Station[] getStations()
        {
            Station[] retval;
            lock(slocker)
            {
                retval = stationsInRoom.ToArray<Station>();
            }
            return retval;
        }
        public Device[] getDevices()
        {
            Device[] retval;
            lock(dlocker)
            {
                retval = devicesInRoom.ToArray<Device>();
            }
            return retval;
        }


    }
}
