using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace Server
{
    class Context
    {
        ConcurrentDictionary<String, Station> stations;
        ConcurrentDictionary<String, PositionTools.Room> rooms;
        ConcurrentDictionaryStack<String, Device> deviceMap;
        ConcurrentDictionary<PositionTools.Room, ConcurrentDictionary<Device,byte>> peoplePerRoom;
        AnalysisEngine analyzer;
        Calibrator calibrator;
        Dictionary<PositionTools.Room, List<Station>> stationsPerRoom;
        ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
        DatabasePublisher databasePub;
        DatabaseInterface databaseInt;
        List<Publisher> publishers;
        Aggregator aggregator;
        public Context()
        {
            stations = new ConcurrentDictionary<String, Station>();
            rooms = new ConcurrentDictionary<String, PositionTools.Room>();
            stationsPerRoom = new Dictionary<PositionTools.Room, List<Station>>();
            deviceMap = new ConcurrentDictionaryStack<string, Device>();
            peoplePerRoom = new ConcurrentDictionary<PositionTools.Room, ConcurrentDictionary<Device,byte>>();
            databaseInt = new DatabaseInterface();
            publishers = new List<Publisher>();
            databasePub = new DatabasePublisher();
            GuiInterface guiPub = new GuiInterface();
            publishers.Add(databasePub);
            publishers.Add(guiPub);
            aggregator = new Aggregator(publishers);
            publishers.Add(aggregator);
            analyzer = new AnalysisEngine(publishers, peoplePerRoom, deviceMap);
            calibrator = new Calibrator(analyzer);            
        }

        public void orchestrate()
        {
            Thread analyzerT = new Thread(new ThreadStart(analyzer.analyzerProcess));
            analyzerT.Start();
            Thread databaseT = new Thread(new ThreadStart(databasePub.databaseProcess));
            databaseT.Start();
            Thread aggregatorT = new Thread(new ThreadStart(aggregator.aggregatorProcess));
            aggregatorT.Start();
            analyzerT.Join();
            calibrator.kill(); //should not be necessary
            //if analyzer completes, kill everything: to shutdown application, kill analyzer!
            databasePub.kill();
            aggregator.kill();
            databaseT.Join();
            aggregatorT.Join();
            Environment.Exit(0);            
        }
        public Analyzer getAnalyzer()
        {
            if (calibrator.inCalibration)
                return calibrator;
            else
                return analyzer;
        }
        public bool switchCalibration(bool calibrate, PositionTools.Room roomToCalibrate)
        {
            bool ris = false;
            if (calibrate && calibrator.switchCalibration(roomToCalibrate))
            {
                Thread calibratorT = new Thread(new ThreadStart(calibrator.calibratorProcess));
                calibratorT.Start();
                ris = true;
            }
            else if (!calibrate && calibrator.switchCalibration(null))
            {
                calibrator.kill();
                ris = true;
            }
            return ris;
        }
        public Station tryAddStation(String NameMAC, StationHandler handler, bool AllowAsynchronous) //Replace Object with the relevant type
        {
            Station s=loadStation(NameMAC,handler);
            if(s==null&&AllowAsynchronous) //this is already the check if a configuration for the station exists or not
            {
                //TODO_FEDE: open GUI, get info, then from that guiThread call createStation & then saveStation
                return null;
            }
            return s;
        }

        public void loadRooms()
        {
            foreach (DatabaseInterface.RoomInfo ri in databaseInt.loadRooms())
                createRoom(ri.RoomName, ri.Xlen, ri.Ylen);
            return;
        }

        public bool saveRoom(PositionTools.Room room)
        {
            return databaseInt.saveRoom(room.roomName,room.xlength,room.ylength);
        }
        public bool deleteRoom(String RoomName)
        {
            return databaseInt.deleteRoom(RoomName);
        }
        public Station loadStation(String NameMAC, StationHandler handler)
        {
            DatabaseInterface.StationInfo? si = databaseInt.loadStationInfo(NameMAC);
            if (si == null)
                return null;
            Station s=new Station();
            s.lastHearthbeat = DateTime.Now;
            s.NameMAC = NameMAC;
            s.handler = handler;
            String roomName=si.Value.RoomName;
            double x=si.Value.X;
            double y=si.Value.Y;
            PositionTools.Room room=getRoom(roomName);
            s.location = new PositionTools.Position(x,y,room);
            s.shortInterpolator=Interpolators.deserialize(si.Value.shortInterpolator);
            s.longInterpolator=Interpolators.deserialize(si.Value.longInterpolator);
            locker.EnterWriteLock();
            stationsPerRoom[room].Add(s);
            stations[s.NameMAC] = s;
            locker.ExitWriteLock();
            foreach (Publisher pb in publishers)
                if (pb.supportsOperation(Publisher.DisplayableType.StationUpdate))
                    pb.publishStationUpdate(room,Publisher.EventType.Appear);
            return s;
        }

        public bool saveStation(Station s)
        {
            DatabaseInterface.StationInfo si = new DatabaseInterface.StationInfo();
            si.NameMAC = s.NameMAC;
            si.RoomName = s.location.room.roomName;
            si.X = s.location.X;
            si.Y = s.location.Y;
            si.shortInterpolator = Interpolators.serialize(s.shortInterpolator);
            si.longInterpolator = Interpolators.serialize(s.longInterpolator);
            return databaseInt.saveStationInfo(si); ;
        }
        public bool deleteStation(String NameMAC)
        {
            return databaseInt.removeStation(NameMAC);
        }

        public Station createStation(PositionTools.Room room, String NameMAC, double X, double Y,StationHandler handler)
        {
            Station s = new Station();
            s.lastHearthbeat = DateTime.Now;
            s.NameMAC = NameMAC;
            s.location = new PositionTools.Position(X, Y, room);
            s.shortInterpolator=PositionTools.StandardShortInterpolator;
            s.longInterpolator= PositionTools.StandardLongInterpolator;
            s.handler = handler;
            locker.EnterWriteLock();
            stationsPerRoom[room].Add(s);
            stations[s.NameMAC] = s;
            locker.ExitWriteLock();
            foreach (Publisher pb in publishers)
                if (pb.supportsOperation(Publisher.DisplayableType.StationUpdate))
                    pb.publishStationUpdate(room,Publisher.EventType.Appear);
            return s;
        }

        public Station getStation(String NameMAC)
        {
            return stations[NameMAC];
        }
        public IEnumerable<PositionTools.Room> getRooms()
        {
            return rooms.Values.ToArray<PositionTools.Room>();
        }
        public PositionTools.Room getRoom(String name)
        {
            return rooms[name];
        }
        public void removeStation(String NameMAC)
        {
            Station s;
            stations.TryRemove(NameMAC,out s);
            PositionTools.Room room=s.location.room;
            locker.EnterWriteLock();
            List<Station> st = stationsPerRoom[room];
            st.Remove(s);
            locker.ExitWriteLock();
            foreach (Publisher pb in publishers)
                if (pb.supportsOperation(Publisher.DisplayableType.StationUpdate))
                    pb.publishStationUpdate(room,Publisher.EventType.Disappear);
        }
        public void removeRoom(String NameMAC)
        {
            PositionTools.Room room = rooms[NameMAC];
            locker.EnterWriteLock();
            foreach (Station s in stationsPerRoom[room].ToArray())
            {
                removeStation(s.NameMAC);
                deleteStation(s.NameMAC);
            }
            rooms.TryRemove(room.roomName,out room);
            locker.ExitWriteLock();
        }
        public bool checkStationAliveness(PositionTools.Room room)
        {
            bool ris = true;
            locker.EnterUpgradeableReadLock();
            List<Station> st = stationsPerRoom[room];
            for (int i = 0; i < st.Count; i++)
            {
                if (st[i].lastHearthbeat.AddMinutes(5)<DateTime.Now)
                {
                    ris = false;
                    removeStation(st[i].NameMAC);
                    break;
                }
            }
            locker.ExitUpgradeableReadLock();
            return ris;
        }

        public int getNumberStationPerRoom(PositionTools.Room room)
        {
            int count;
            locker.EnterReadLock();
            count = stationsPerRoom[room].Count;
            locker.ExitReadLock();
            return count;
        }

        public IEnumerable<Station> getStationsInRoom(PositionTools.Room room)
        {
            Station[] ris;
            locker.EnterReadLock();
            ris = stationsPerRoom[room].ToArray();
            locker.ExitReadLock();
            return ris;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="xl"></param>
        /// <param name="yl"></param>
        /// <param name="saveToFile"></param>
        /// <returns>The created/instanciated Room object, null if it already exists</returns>
        public PositionTools.Room createRoom(String name, double xl, double yl)
        {
            //init station per room, user per room
            PositionTools.Room r = new PositionTools.Room();
            r.roomName = name;
            r.xlength = xl;
            r.ylength = yl;
            List<Station> ls = new List<Station>();
            locker.EnterWriteLock();
            if (rooms.ContainsKey(name))
                r = null;
            else
            {
                rooms[name] = r;
                stationsPerRoom[r] = ls;
            }
            locker.ExitWriteLock();
            if(r!=null)
                peoplePerRoom.TryAdd(r, new ConcurrentDictionary<Device,byte>());
            foreach (Publisher pb in publishers)
                if (pb.supportsOperation(Publisher.DisplayableType.RoomUpdate))
                    pb.publishRoomUpdate(r,Publisher.EventType.Appear);
            return r;
        }


    }
}
