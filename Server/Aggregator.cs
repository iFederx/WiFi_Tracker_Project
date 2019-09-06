using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Panopticon
{
    class Aggregator : Publisher
    {
        private class AvgBucket
        {
            internal double tenminutestotal;
            internal double onesecond;
            internal int count;
            internal DateTime tenminutelast;
            internal Room room;
            internal bool dirty;
        }
        ConcurrentDictionary<Room,AvgBucket> stats=new ConcurrentDictionary<Room, AvgBucket>();
        List<Publisher> propagate;
        volatile Boolean killed = false;

        public Aggregator(List<Publisher> p)
        {
            propagate = new List<Publisher>();
            propagate.AddRange(p);

        }
        internal override int SupportedOperations
        {
            get
            {
                return (int)DisplayableType.SimpleStat | (int) DisplayableType.RoomUpdate;
            }
        }
        internal override void publishRoomUpdate(Room r, EventType e)
        {
            AvgBucket ab;
            if (e==EventType.Appear)
            {
                ab = new AvgBucket();
                lock (ab)
                {
                    ab.tenminutestotal = 0;
                    ab.onesecond = 0;
                    ab.room = r;
                    ab.count = 0;
                    ab.dirty = true;
                    ab.tenminutelast = DateTime.Now;
                }                
                stats.TryAdd(r, ab);
            }
            else if(e==EventType.Disappear)
            {
                stats.TryRemove(r,out ab);
                updateRoomStat(ab, ab.onesecond, DateTime.Now);
                foreach (Publisher pb in propagate)
                    pb.publishStat(ab.count>0?ab.tenminutestotal/ab.count:0, ab.room, DateTime.Now, StatType.TenMinuteAverageDeviceCount);
            }
        }

        //2 aggregates: 10 minute average, one second value
        internal override void publishStat(double stat, Room r, DateTime statTime, StatType s) 
        {
            AvgBucket ab;
            if (s != StatType.InstantaneousDeviceCount)
                throw new NotSupportedException();
            if (!stats.ContainsKey(r))
                return;
            ab = stats[r];
            updateRoomStat(ab,stat,statTime);
            System.Diagnostics.Debug.Print("ROOM STAT: " + r.roomName + " count: " + stat);
        }

        private void updateRoomStat(AvgBucket ab, double instantaneousPeopleCount, DateTime statTime)
        {
            lock(ab)
            {
                ab.onesecond = instantaneousPeopleCount;
                ab.dirty = true;
            }            
        }

        internal void aggregatorProcess()
        {
            while(!killed)
            {
                Thread.Sleep(1000);
                foreach(AvgBucket ab in stats.Values)
                {
                    lock(ab)
                    {
                        ab.count++;
                        ab.tenminutestotal += ab.onesecond;
                        if (ab.dirty)
                            foreach (Publisher pb in propagate)//supports stat by default, or wouldn't be here
                                pb.publishStat(ab.onesecond, ab.room, DateTime.Now, StatType.OneSecondDeviceCount);
                       if(ab.tenminutelast.AddMinutes(10)<DateTime.Now)
                       {
                            foreach (Publisher pb in propagate)
                                pb.publishStat(ab.count>0?ab.tenminutestotal/ab.count:0, ab.room, DateTime.Now, StatType.TenMinuteAverageDeviceCount);
                            ab.tenminutestotal = 0;
                            ab.count = 0;
                            ab.tenminutelast = DateTime.Now;
                        }
                        ab.dirty = false;                            
                    }
                }
            }
        }
        internal void kill()
        {
            killed = true;
        }
    }
}
