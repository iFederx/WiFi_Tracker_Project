﻿using System;
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
		Connection connection;
		LinkedList<Thread> threads = new LinkedList<Thread>();
        internal readonly GuiInterface guiPub;
		
        public Context()
        {
            stations = new ConcurrentDictionary<String, Station>();
            deviceMap = new ConcurrentDictionaryStack<String, Device>();
            rooms = new ConcurrentDictionary<String, Room>();
            try
            {
                databaseInt = new DatabaseInterface(Properties.Settings.Default.ConnectionString);
            }
            catch
            {
                System.Environment.Exit(1);
                return;
            }
            publishers = new List<Publisher>();
            databasePub = new DatabasePublisher(databaseInt);
            publishers.Add(databasePub);
            guiPub = new GuiInterface();
            publishers.Add(guiPub);
            aggregator = new Aggregator(publishers);
            publishers.Add(aggregator);
            analyzer = new AnalysisEngine(publishers, deviceMap);
            //calibrator = new Calibrator(analyzer); //DARIO: da merge, probabilmente da rimuovere
			packetizer = new FileParser(this);
			connection = new Connection(this);
		}

        public void orchestrate()
        {
            Thread socketListenerT = new Thread(new ThreadStart(connection.StartConnection));
			socketListenerT.Name = "Server thread";
			socketListenerT.Start();
			Thread analyzerT = new Thread(new ThreadStart(analyzer.analyzerProcess));
            analyzerT.Name = "Analyzer thread";
            analyzerT.Start();
            Thread aggregatorT = new Thread(new ThreadStart(aggregator.aggregatorProcess));
            aggregatorT.Name = "Aggregator thread";
            aggregatorT.Start();
			//TODO: da rimuovere a migrazione "on the fly" avvenuta
			//Thread packetizerT = new Thread(new ThreadStart(packetizer.packetizerProcess));
			//packetizerT.Name = "Packetizer thread";
			//packetizerT.Start();
			threads.AddLast(socketListenerT);
			threads.AddLast(analyzerT);
            threads.AddLast(aggregatorT);
			//threads.AddLast(packetizerT);
		}
        public AnalysisEngine getAnalyzer()
        {
           return analyzer;
        }
       
        public Station tryAddStation(String NameMAC, StationHandler _handler, bool AllowAsynchronous) //Replace Object with the relevant type
        {
            // check if not attempting reconnection
            locker.EnterReadLock(); //lock to avoid removal if reconnecting
            Station s = getStation(NameMAC);
            if(s!=null) // s is reconnecting after losing contact
            {
				//DONE_FEDE: what to do with the old and the new station handler?
				s.handler.socket.Close();
				s.handler = _handler; //updating handler
                s.hearbeat();
                locker.ExitReadLock();
                return s;
            }
            locker.ExitReadLock();
            s = loadStation(NameMAC, _handler); //search Station in DB
            if (s==null && AllowAsynchronous) //this is already the check if a configuration for the station exists or not
            {
				StationAdder sa1 = new StationAdder(this, _handler);
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
            return databaseInt.saveRoom(room.roomName,room.size.X,room.size.Y);
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
            s.hearbeat();
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
            return stations.ContainsKey(NameMAC)?stations[NameMAC]:null;
        }
		public bool StationConfigured(String NameMAC)
		{
			if (stations.ContainsKey(NameMAC))
				return true;
			else return false;
		}
        public IEnumerable<Room> getRooms()
        {
            return rooms.Values.ToArray<Room>();
        }
        public Room getRoom(String name)
        {
            return rooms[name];
        }
        public void removeStation(String NameMAC, bool takelock=true)
        {
            Station s;
            stations.TryRemove(NameMAC, out s);
            Room room=s.location.room;
            if(takelock)
                locker.EnterWriteLock();
            room.removeStation(s);
            if(takelock)
                locker.ExitWriteLock();
            foreach (Publisher pb in publishers)
                if (pb.supportsOperation(Publisher.DisplayableType.StationUpdate))
                    pb.publishStationUpdate(room,s,Publisher.EventType.Disappear);
        }
        public void removeRoom(String RoomName)
        {
            Room room = rooms[RoomName];
            locker.EnterWriteLock();
            foreach (Station s in room.getStations())
            {
                removeStation(s.NameMAC, false);
                deleteStation(s.NameMAC);
				s.handler.reboot();
            }
            rooms.TryRemove(room.roomName, out room);
            locker.ExitWriteLock();
            foreach (Publisher pb in publishers)
                if (pb.supportsOperation(Publisher.DisplayableType.RoomUpdate))
                    pb.publishRoomUpdate(room, Publisher.EventType.Disappear);
        }
        public bool checkStationAliveness(Room room)
        {
            bool ris = true;
            locker.EnterUpgradeableReadLock();
            foreach(Station s in room.getStations())
            {
                if (s.lastHearthbeat.AddMinutes(5)<DateTime.Now)
                {
                    locker.EnterWriteLock();
                    if(s.lastHearthbeat.AddMinutes(5) < DateTime.Now) // check again: was in readmode, may have reconnected now and updated
                    {
                        ris = false;
                        removeStation(s.NameMAC, false);
                        // D: here there was a break. why? for fear of modifying the iterating collection? but it should already have been cached
						// F: it was only for testing this portion of code. Delete all comments when you see ;)
                    }
                    locker.ExitWriteLock();
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
            guiPub.kill();
            analyzer.kill();
            aggregator.kill();
            databasePub.kill();
            foreach(Thread t in threads)
            {
                t.Join();
            }
            foreach (Room r in getRooms())
            {
                foreach (Device d in r.getDevices())
                    databaseInt.addDevicePosition(d.identifier, d.MAC, r.roomName, 0, 0, 0, DateTime.Now, Publisher.EventType.Disappear); //sync push on db
                foreach (Station s in r.getStations())
                    s.handler.reboot();
            }
            databasePub.confirmclose(); // wait that everything has been written to the db before killing.
            databaseInt.close();
            Environment.Exit(0);
        }


    }
}
