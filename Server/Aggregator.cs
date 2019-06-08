using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Aggregator : Publisher
    {
        private class AvgBucket
        {
            internal double avg;
            internal double lastval;
            internal DateTime lastvalTime;
            internal DateTime avgTimeBase;
            internal PositionTools.Room room;
            internal bool dirty;
        }
        ConcurrentDictionary<PositionTools.Room,AvgBucket> stats=new ConcurrentDictionary<PositionTools.Room, AvgBucket>();
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
        internal override void publishRoomUpdate(PositionTools.Room r, EventType e)
        {
            AvgBucket ab;
            if (e==EventType.Appear)
            {
                ab = new AvgBucket();
                lock (ab)
                {
                    ab.avg = 0;
                    ab.lastval = 0;
                    ab.room = r;
                    ab.lastvalTime = DateTime.Now;
                    ab.dirty = true;
                }                
                stats.TryAdd(r, ab);
            }
            else if(e==EventType.Disappear)
            {
                stats.TryRemove(r,out ab);
                updateRoomStat(ab, ab.lastval, DateTime.Now);
                foreach (Publisher pb in propagate)
                    pb.publishStat(ab.avg, ab.room, ab.lastvalTime, StatType.TenMinuteAveragePeopleCount);
            }
        }

        //2 aggregates: 10 minute average, one second value
        internal override void publishStat(double stat, PositionTools.Room r, DateTime statTime, StatType s) 
        {
            AvgBucket ab;
            if (s != StatType.InstantaneousPeopleCount)
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
                ab.avg = (ab.avg * (ab.lastvalTime - ab.avgTimeBase).TotalMilliseconds + ab.lastval * (statTime - ab.lastvalTime).TotalMilliseconds) / (statTime - ab.avgTimeBase).TotalMilliseconds;
                ab.lastval = instantaneousPeopleCount;
                ab.lastvalTime = statTime;
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
                        if (ab.dirty)
                            foreach (Publisher pb in propagate)//supports stat by default, or wouldn't be here
                                pb.publishStat(ab.lastval, ab.room, ab.lastvalTime, StatType.OneSecondPeopleCount);
                        if (ab.lastvalTime >= ab.avgTimeBase.AddMinutes(10))
                        {
                            foreach (Publisher pb in propagate)
                                pb.publishStat(ab.avg, ab.room, ab.lastvalTime, StatType.TenMinuteAveragePeopleCount);
                            ab.avg = ab.lastval;
                            ab.avgTimeBase = ab.lastvalTime;
                        }
                        else if (ab.lastvalTime.AddMinutes(10) <= DateTime.Now)
                        {
                            DateTime newTime= ab.avgTimeBase.AddMinutes(10);
                            ab.avg = (ab.avg * (ab.lastvalTime - ab.avgTimeBase).TotalMilliseconds + ab.lastval * (newTime - ab.lastvalTime).TotalMilliseconds) / (newTime - ab.avgTimeBase).TotalMilliseconds;
                            ab.lastvalTime = newTime;
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
