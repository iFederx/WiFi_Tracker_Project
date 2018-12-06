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
        Dictionary<String, Station> stations;
        ConcurrentDictionaryStack<String, Device> deviceMap;
        ConcurrentDictionary<String, ConcurrentDictionary<Device, Byte>> peoplePerRoom;
        volatile bool inCalibration;
        AnalysisEngine analyzer;
        Calibrator calibrator;
        Dictionary<String, List<Station>> stationsPerRoom;
        ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
       
        public Context()
        {
            stations = new Dictionary<String, Station>();
            deviceMap = new ConcurrentDictionaryStack<String, Device>();
            stationsPerRoom = new Dictionary<String, List<Station>>();
            peoplePerRoom = new ConcurrentDictionary<string, ConcurrentDictionary<Device, byte>>();
            analyzer = new AnalysisEngine(deviceMap,peoplePerRoom);
            calibrator = new Calibrator();
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
            inCalibration = calibrate;
            //open calibration GUI
        }
        public void tryAddStation(String NameMAC)
        {
            //search file, if not found ask main thread to open GUI , that will then call addStation 
        }
        public void addStation(Station s)
        {
            locker.EnterWriteLock();
            //add station to room. If there are at least 3 in the room, and even just one has no interpolator, call calibrator (outside the lock)
            locker.ExitWriteLock();
        }
        public Station getStation(String NameMAC)
        {
            locker.EnterReadLock();
            locker.ExitReadLock();
        }
        public void checkStationAliveness(int Room)
        {
            locker.EnterUpgradeableReadLock();
            locker.ExitUpgradeableReadLock();
        }

        public int getNumberStationPerRoom(String Room)
        {
            locker.EnterReadLock();
            locker.ExitReadLock();
        }
        public void createRoom(String Room)
        {
            //init station per room, user per room
        }

    }
}
