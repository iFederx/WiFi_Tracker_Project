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
        volatile bool inCalibration;
        AnalysisEngine analyzer;
        Calibrator calibrator;
        Dictionary<PositionTools.Room, List<Station>> stationsPerRoom;
        ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
        DatabaseInterface databaseInt;
        public Context()
        {
            stations = new ConcurrentDictionary<String, Station>();
            rooms = new ConcurrentDictionary<String, PositionTools.Room>();
            stationsPerRoom = new Dictionary<PositionTools.Room, List<Station>>();
            List<Publisher> pb = new List<Publisher>();
            databaseInt = new DatabaseInterface();
            GuiInterface guiInt = new GuiInterface();
            pb.Add(databaseInt);
            pb.Add(guiInt);
            Aggregator ag = new Aggregator(pb);
            pb.Add(ag);
            analyzer = new AnalysisEngine(pb);
            calibrator = new Calibrator(analyzer);            
        }

        public void orchestrate()
        {
            Thread analyzerT = new Thread(new ThreadStart(analyzer.analyzerProcess));
            analyzerT.Start();
            Thread calibratorT = new Thread(new ThreadStart(calibrator.calibratorProcess));
            calibratorT.Start();
            Thread databaseT = new Thread(new ThreadStart(databaseInt.databaseProcess));
            databaseT.Start();
            analyzerT.Join();
            //if analyzer completes, kill everything: to shutdown application, kill analyzer!
            databaseInt.kill();
            calibrator.kill();
            calibratorT.Join();
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
        public void switchCalibration(bool calibrate)
        {
            //TODO
            inCalibration = calibrate;
            //open calibration GUI
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
            //add station to room. If there are at least 3 in the room, and even just one has no interpolator, call calibrator (outside the lock)
            locker.ExitWriteLock();
        }
        public Station getStation(String NameMAC)
        {
            return stations[NameMAC];
        }
        public void checkStationAliveness(PositionTools.Room room)
        {
            //TODO
            locker.EnterUpgradeableReadLock();
            locker.ExitUpgradeableReadLock();
        }

        public int getNumberStationPerRoom(PositionTools.Room room)
        {
            int count;
            locker.EnterReadLock();
            count = stationsPerRoom[room].Count;
            locker.ExitReadLock();
            return count;
        }
        public bool createRoom(String name, double xl, double yl)
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
            return ris;
        }


    }
}
