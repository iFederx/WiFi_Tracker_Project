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
        volatile bool inCalibration;
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
            //if analyzer completes, kill everything: to shutdown application, kill analyzer!
            databaseInt.kill();
            databaseT.Join();
            Environment.Exit(0);            
        }
        public Analyzer getAnalyzer()
        {
            if (inCalibration)
                return calibrator;
            else
                return analyzer;
        }
        public bool switchCalibration(bool calibrate, PositionTools.Room roomToCalibrate)
        {
            if (!Interlocked.CompareExchange(ref inCalibration, calibrate, !calibrate) ^ calibrate)
                return false; //already in calibration if calibration requested, or not in calibration if calibration switchoff requested
            if(calibrate)
            {
                Thread calibratorT = new Thread(new ThreadStart(calibrator.calibratorProcess));
                calibratorT.Start();
            }
            else
                calibrator.kill();
            //TODO
            return true;
        }
        public void tryAddStation(String NameMAC)
        {
            //TODO
            //search file, if not found ask main thread to open GUI , that will then call addStation 
        }
        public void addStation(String roomName, Station s)
        {
            PositionTools.Room r = rooms[roomName];
            s.location.room = r;
            locker.EnterWriteLock();
            stationsPerRoom[r].Add(s);
            stations[s.NameMAC] = s;
            //add station to room. If there are at least 3 in the room, and even just one has no interpolator, switch calibrator on (outside the lock)
            //TODO
            locker.ExitWriteLock();
        }
        public Station getStation(String NameMAC)
        {
            return stations[NameMAC];
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
                    locker.EnterWriteLock();
                    st.RemoveAt(i);
                    locker.ExitWriteLock();
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
        public bool createRoom(String name, double xl, double yl, bool saveToFile)
        {
            //init station per room, user per room
            bool ris;
            PositionTools.Room r = new PositionTools.Room();
            r.roomName = name;
            r.xlength = xl;
            r.ylength = yl;
            List<Station> ls = new List<Station>();
            locker.EnterWriteLock();
            if (rooms.ContainsKey(name))
                ris = false;
            else
            {
                rooms[name] = r;
                stationsPerRoom[r] = ls;
                ris = true;
            }
            locker.ExitWriteLock();
            if(ris)
                peoplePerRoom.TryAdd(r, new ConcurrentDictionary<Device,byte>());
            if (saveToFile)
                int TODO;
                //TODO_ENRICO: save the room info to file
            return ris;
        }


    }
}
