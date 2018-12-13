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
        DatabasePublisher databaseInt;
        public Context()
        {
            stations = new ConcurrentDictionary<String, Station>();
            rooms = new ConcurrentDictionary<String, PositionTools.Room>();
            stationsPerRoom = new Dictionary<PositionTools.Room, List<Station>>();
            deviceMap = new ConcurrentDictionaryStack<string, Device>();
            peoplePerRoom = new ConcurrentDictionary<PositionTools.Room, ConcurrentDictionary<Device,byte>>();
            List<Publisher> pb = new List<Publisher>();
            databaseInt = new DatabasePublisher();
            GuiInterface guiInt = new GuiInterface();
            pb.Add(databaseInt);
            pb.Add(guiInt);
            Aggregator ag = new Aggregator(pb);
            pb.Add(ag);
            analyzer = new AnalysisEngine(pb,peoplePerRoom,deviceMap);
            calibrator = new Calibrator(analyzer);            
        }

        public void orchestrate()
        {
            Thread analyzerT = new Thread(new ThreadStart(analyzer.analyzerProcess));
            analyzerT.Start();
            Thread databaseT = new Thread(new ThreadStart(databaseInt.databaseProcess));
            databaseT.Start();
            analyzerT.Join();
            calibrator.kill(); //should not be necessary
            //if analyzer completes, kill everything: to shutdown application, kill analyzer!
            databaseInt.kill();
            databaseT.Join();
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
        public Station tryAddStation(String NameMAC, Object LedBlinkHandle, bool AllowAsynchronous) //Replace Object with the relevant type
        {
            Station s=loadStation(NameMAC);
            if(s==null&&AllowAsynchronous) //this is already the check if a configuration for the station exists or not
            {
                //TODO_FEDE: open GUI, get info, then from that guiThread call createStation
                return null;
            }
            return s;
        }

        public void loadRooms()
        {
            //TODO_FEDE
            return;
        }

        public bool saveRoom(PositionTools.Room room)
        {
            //TODO_FEDE
            return false;
        }

        public Station loadStation(String NameMAC)
        {
            //TODO_FEDE: check if conf file for NameMAC Exists. If not, return null
            Station s=new Station();
            s.lastHearthbeat = DateTime.Now;
            s.NameMAC = NameMAC;
            //TODO_FEDE: load Room name, position in the room
            String roomName=null; //replace with loaded value
            double x=101; //replace with loaded value
            double y=101; //replace with loaded value
            PositionTools.Room room=getRoom(roomName);
            s.location = new PositionTools.Position(x,y,room);
            //TODO_FEDE: load the interpolators for this station
            s.shortInterpolator=null; //replace with loaded value
            s.longInterpolator=null; //replace with loaded value
            locker.EnterWriteLock();
            stationsPerRoom[room].Add(s);
            stations[s.NameMAC] = s;
            locker.ExitWriteLock();
            return s;
        }

        public Station saveStation(Station s)
        {
            //TODO_FEDE
            // important: for the station.location.room do not save the object but the name of the room
            // Interpolators are serializable values, so saving and loading should be quick, no specific code needed
            return null;
        }

        public Station createStation(PositionTools.Room room, String NameMAC, double X, double Y)
        {
            Station s = new Station();
            s.lastHearthbeat = DateTime.Now;
            s.NameMAC = NameMAC;
            s.location = new PositionTools.Position(X, Y, room);
            s.shortInterpolator=Calibrator.stdShort;
            s.longInterpolator=Calibrator.stdLong;
            locker.EnterWriteLock();
            stationsPerRoom[room].Add(s);
            stations[s.NameMAC] = s;
            locker.ExitWriteLock();
            return s;
        }

        public Station getStation(String NameMAC)
        {
            return stations[NameMAC];
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
            return r;
        }


    }
}
