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
        public void tryAddStation(String NameMAC)
        {
            //TODO_FEDE: search file, if exists load it and call createStation (and load & attach the interpolators to the created station), if not open GUI, get info, then from that guiThread call createStation
        }
        public Station createStation(PositionTools.Room room, String NameMAC, double X, double Y)
        {
            Station s = new Station();
            s.lastHearthbeat = DateTime.Now;
            s.NameMAC = NameMAC;
            s.location = new PositionTools.Position(X, Y, room);
            locker.EnterWriteLock();
            stationsPerRoom[room].Add(s);
            stations[s.NameMAC] = s;
            Boolean mustCalibrate = false;
            foreach(Station st in stationsPerRoom[room])
            {
                if(st.shortInterpolator==null||st.longInterpolator==null)
                {
                    mustCalibrate = true;
                }
            }
            mustCalibrate = mustCalibrate && stationsPerRoom[room].Count >= 3;
            locker.ExitWriteLock();
            if (mustCalibrate)
                switchCalibration(true, room);
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
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="xl"></param>
        /// <param name="yl"></param>
        /// <param name="saveToFile"></param>
        /// <returns>The created/instanciated Room object, null if it already exists</returns>
        public PositionTools.Room createRoom(String name, double xl, double yl, bool saveToFile)
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
            if (saveToFile)
            {
                //TODO_FEDE: save the room info to file
            }
            return r;
        }


    }
}
