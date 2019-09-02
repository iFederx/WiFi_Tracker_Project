using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace Panopticon
{
    class Context
    {
        ConcurrentDictionary<String, Station> stations;
        ConcurrentDictionary<String, Room> rooms;
        ConcurrentDictionaryStack<String, Device> deviceMap;
        AnalysisEngine analyzer;
        ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
        DatabasePublisher databasePub;
        internal readonly DatabaseInterface databaseInt;
        List<Publisher> publishers;
        Aggregator aggregator;
		internal FileParser packetizer;
        LinkedList<Thread> threads = new LinkedList<Thread>();
        internal readonly GuiInterface guiPub;
		
        public Context()
        {
            stations = new ConcurrentDictionary<String, Station>();
            deviceMap = new ConcurrentDictionaryStack<String, Device>();
            rooms = new ConcurrentDictionary<String, Room>();
            databaseInt = new DatabaseInterface(Properties.Settings.Default.ConnectionString);
            publishers = new List<Publisher>();
            databasePub = new DatabasePublisher(databaseInt);
            publishers.Add(databasePub);
            guiPub = new GuiInterface();
            publishers.Add(guiPub);
            aggregator = new Aggregator(publishers);
            publishers.Add(aggregator);
            analyzer = new AnalysisEngine(publishers, deviceMap);
            //calibrator = new Calibrator(analyzer);
			packetizer = new FileParser(this);
        }

        public void orchestrate()
        {
            Thread analyzerT = new Thread(new ThreadStart(analyzer.analyzerProcess));
            analyzerT.Start();
            Thread databaseT = new Thread(new ThreadStart(databasePub.databaseProcess));
            databaseT.Start();
            Thread aggregatorT = new Thread(new ThreadStart(aggregator.aggregatorProcess));
            aggregatorT.Start();
			Thread packetizerT = new Thread(new ThreadStart(packetizer.packetizerProcess));
			packetizerT.Start();
            threads.AddLast(analyzerT);
            threads.AddLast(databaseT);
            threads.AddLast(aggregatorT);
			threads.AddLast(packetizerT);
		}
        public Analyzer getAnalyzer()
        {
           return analyzer;
        }
       
        public Station tryAddStation(String NameMAC, StationHandler handler, bool AllowAsynchronous) //Replace Object with the relevant type
        {
            Station s = loadStation(NameMAC, handler);
            if (s==null && AllowAsynchronous) //this is already the check if a configuration for the station exists or not
            {
				//DONE_FEDE: open GUI, get info, then from that guiThread call createStation & then saveStation
				StationAdder sa1 = new StationAdder(this, handler);
				sa1.Show();
				return null; //TODO: è normale che se creo la station da GUI ritorno null?
            }
            return s;
        }

        public void loadRooms()
        {
            foreach (DatabaseInterface.RoomInfo ri in databaseInt.loadRooms())
                createRoom(new Room(ri.RoomName, ri.Xlen, ri.Ylen));
            createRoom(Room.externRoom);
            return;
        }

        public bool saveRoom(Room room)
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
            Room room=getRoom(roomName);
            s.location = new PositionTools.Position(x,y,room);
            locker.EnterWriteLock();
            room.addStation(s);
            stations[s.NameMAC] = s;
            locker.ExitWriteLock();
            foreach (Publisher pb in publishers)
                if (pb.supportsOperation(Publisher.DisplayableType.StationUpdate))
                    pb.publishStationUpdate(room,s,Publisher.EventType.Appear);
            return s;
        }

        public bool saveStation(Station s)
        {
            DatabaseInterface.StationInfo si = new DatabaseInterface.StationInfo();
            si.NameMAC = s.NameMAC;
            si.RoomName = s.location.room.roomName;
            si.X = s.location.X;
            si.Y = s.location.Y;
            return databaseInt.saveStationInfo(si); ;
        }
        public bool deleteStation(String NameMAC)
        {
            return databaseInt.removeStation(NameMAC);
        }

        public Station createStation(Room room, String NameMAC, double X, double Y, StationHandler handler)
        {
            Station s = new Station();
            s.lastHearthbeat = DateTime.Now;
            s.NameMAC = NameMAC;
            s.location = new PositionTools.Position(X, Y, room);
            s.handler = handler;
            locker.EnterWriteLock();
            room.addStation(s);
            stations[s.NameMAC] = s;
            locker.ExitWriteLock();
            foreach (Publisher pb in publishers)
                if (pb.supportsOperation(Publisher.DisplayableType.StationUpdate))
                    pb.publishStationUpdate(room,s,Publisher.EventType.Appear);
            return s;
        }

        public Station getStation(String NameMAC)
        {
            return stations[NameMAC]; //DARIO: KeyNotFoundException: all'improvviso stations era vuota
        }
        public IEnumerable<Room> getRooms()
        {
            return rooms.Values.ToArray<Room>();
        }
        public Room getRoom(String name)
        {
            return rooms[name];
        }
        public void removeStation(String NameMAC)
        {
            Station s;
            stations.TryRemove(NameMAC,out s);
            Room room=s.location.room;
            locker.EnterWriteLock();
            room.removeStation(s);
            locker.ExitWriteLock();
            foreach (Publisher pb in publishers)
                if (pb.supportsOperation(Publisher.DisplayableType.StationUpdate))
                    pb.publishStationUpdate(room,s,Publisher.EventType.Disappear);
        }
        public void removeRoom(String NameMAC)
        {
            Room room = rooms[NameMAC];
            locker.EnterWriteLock();
            foreach (Station s in room.getStations())
            {
                removeStation(s.NameMAC);
                deleteStation(s.NameMAC);
            }
            rooms.TryRemove(room.roomName,out room);
            locker.ExitWriteLock();
        }
        public bool checkStationAliveness(Room room)
        {
            bool ris = true;
            locker.EnterUpgradeableReadLock();
            foreach(Station s in room.getStations())
            {
                if (s.lastHearthbeat.AddMinutes(5)<DateTime.Now)
                {
                    ris = false;
                    removeStation(s.NameMAC);
                    break;
                }
            }
            locker.ExitUpgradeableReadLock();
            return ris;
        }
        
        public Room createRoom(Room r)
        {
            //init station per room, user per room
            locker.EnterWriteLock();
            if (rooms.ContainsKey(r.roomName))
                r = null;
            else
            {
                rooms[r.roomName] = r;
            }
            locker.ExitWriteLock();
            foreach (Publisher pb in publishers)
                if (pb.supportsOperation(Publisher.DisplayableType.RoomUpdate))
                    pb.publishRoomUpdate(r,Publisher.EventType.Appear);
            return r;
        }

        public void kill()
        {
            analyzer.kill();
            databasePub.kill();
            aggregator.kill();
			packetizer.kill();
            foreach(Thread t in threads)
            {
                t.Join();
            }
            databaseInt.close();
            Environment.Exit(0);
        }


    }
}
