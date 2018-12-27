using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class DatabaseInterface
    {
        internal struct StationInfo
        {
            internal String NameMAC;
            internal String RoomName;
            internal Double X;
            internal Double Y;
            internal Byte[] shortInterpolator;
            internal Byte[] longInterpolator;
        }
        internal struct RoomInfo
        {
            internal String RoomName;
            internal double Xlen;
            internal double Ylen;
        }
        internal Nullable<StationInfo> loadStationInfo(String NameMAC)
        {
            StationInfo si=new StationInfo();
            return si;
        }

        internal bool saveStationInfo(StationInfo si)
        {
            return true;
        }

        internal IEnumerable<RoomInfo> loadRooms()
        {
            return null;
        }

        internal bool saveRoom(String RoomName, double Xlen, double Ylen)
        {
            return true;
        }

        internal bool deleteRoom(string roomName)
        {
            throw new NotImplementedException();
        }

        internal bool removeStation(string nameMAC)
        {
            throw new NotImplementedException();
        }
    }
}
