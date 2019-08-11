using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Panopticon
{
    class AnalysisEngine:Analyzer
    {
        ConcurrentDictionaryStack<String, Device> deviceMap; //id, corresponding device
        BlockingCollection<Packet> AnalysisQueue = new BlockingCollection<Packet>(new ConcurrentQueue<Packet>());
        Dictionary<String, String> anoniDevices = new Dictionary<string, string>(); //mac, corresponding id
        volatile CancellationTokenSource t;
        volatile bool killed = false;
        //DEBUG
        double totalerror = 0;
        int nerrors = 0;
        List<Publisher> publishers;
        public AnalysisEngine(List<Publisher> pb, ConcurrentDictionaryStack<String, Device> dm)
        {
            publishers = new List<Publisher>(pb);
            deviceMap = dm;

        }
        /// <summary>
        /// Lanciare questo metodo ogni qual volta un pacchetto è "maturo" (ovvero ricevuto da tutte le stazioni o andato in timeout).
        /// </summary>
        /// <param name="p">Pacchetto da analizzare</param>
        /// <param name="c">Contesto</param>
        public void sendToAnalysisQueue(Packet p)
        {
            AnalysisQueue.Add(p);
        }
        public void analyzerProcess()
        {
            t = new CancellationTokenSource(10000);
            while (!killed)
            {
                Packet p;
                try
                {
                    p = AnalysisQueue.Take(t.Token);
                    System.Diagnostics.Debug.Print("Packet popped");
                    analyzePacket(p);                    
                }
                catch (OperationCanceledException)
                {
                    coalesceAnonymous();
                    householdCleaning();
                    t = new CancellationTokenSource(10000);
                }
            }
        }
        private void analyzePacket(Packet p)
        {
            Device d;
            bool anew;
            String id = p.SendingMAC;
            String id2;
            if (anoniDevices.TryGetValue(id, out id2))
                id = id2;
            if (anew=!deviceMap.getKey(id, out d))
            {
                d = new Device();
                d.MAC = p.SendingMAC;
                d.identifier = d.MAC;
                d.HTCapabilities = p.HTCapabilities;
                if(isMACLocal(p.SendingMAC))
                {
                    d.anonymous = true;
                    d.identifier = "ANON-" +DateTime.Now.ToShortDateString()+"."+DateTime.Now.ToShortTimeString()+"-"+ d.MAC.GetHashCode().ToString("X").Substring(0,4);
                    anoniDevices.Add(p.SendingMAC, d.identifier);
                }
            }
            if (p.RequestedSSID != null && !d.requestedSSIDs.ContainsKey(p.RequestedSSID))
            {
                d.requestedSSIDs.TryAdd(p.RequestedSSID,0);
                foreach (Publisher pb in publishers)
                    if (pb.supportsOperation(Publisher.DisplayableType.SSID))
                        pb.publishSSID(d,p.RequestedSSID);
            }
            if (d.lastPosition!=null && d.lastPosition.positionDate.AddSeconds(3) > p.Timestamp && d.lastPosition.room==p.Receivings[0].ReceivingStation.location.room)
                return;
            locateAndPublish(d, p);
            //DEBUG
            double error = Math.Sqrt(Math.Pow((d.lastPosition.X - p.testposition.X),2) + Math.Pow((d.lastPosition.Y - p.testposition.Y),2));
            nerrors++;
            totalerror += error;
            System.Diagnostics.Debug.Print("Error: " + error);
            System.Diagnostics.Debug.Print("Mean Error: " + totalerror/nerrors);
            deviceMap.upsert(d.identifier, d, (old, cur) => { return cur; });//single thread safe only. To make it multithread i should also copy other fields
        }

        private void coalesceAnonymous()
        {
            List<String> anoni=anoniDevices.Values.ToList<String>();
            Device A=null;
            Device B=null;
            Device first;
            Device second;
            int maxpoint;
            Device bestMatch=null;
            int curpoint;
            for(int i=0;i<anoni.Count;i++)
            {
                deviceMap.getKey(anoni[i], out A);
                maxpoint = 0;
                for(int j=i+1;j<anoni.Count;j++)
                {
                    deviceMap.getKey(anoni[j], out B);
                    curpoint = 0;
                    if(A.firstPosition.positionDate>B.firstPosition.positionDate)
                    {
                        second = A;
                        first = B;
                    }
                    else
                    {
                        first = A;
                        second = B;
                    }
                    if (first.lastPosition.positionDate.AddMinutes(3) > DateTime.Now || first.lastPosition.positionDate > second.firstPosition.positionDate || first.lastPosition.room!=second.firstPosition.room)
                        continue;
                    curpoint += Math.Max(0, 70 - (int)second.firstPosition.positionDate.Subtract(first.lastPosition.positionDate).TotalSeconds);
                    if(first.lastPosition.uncertainity!=double.MaxValue&&second.firstPosition.uncertainity!=double.MaxValue)
                        curpoint += Math.Max(0, 40 - 7 * (int)first.lastPosition.Subtract(second.firstPosition).Module());
                    if (Math.Abs(Convert.ToInt64(first.MAC, 16) - Convert.ToInt64(second.MAC, 16)) < 2)
                        curpoint += 100;
                    if (first.HTCapabilities != null && first.HTCapabilities == second.HTCapabilities)
                        curpoint += 30;
                    if (curpoint>maxpoint)
                    {
                        maxpoint = Math.Min(100,curpoint);
                        bestMatch = B;
                    }
                }
                if(maxpoint>50)
                {
                    B = bestMatch;
                    if(A.firstPosition.positionDate>bestMatch.firstPosition.positionDate)
                    {
                        B = A;
                        A = bestMatch;
                    }
                    PositionTools.Position oldApos = new PositionTools.Position(A.lastPosition);
                    oldApos.positionDate = B.firstPosition.positionDate;
                    placeInRoomAndPublish(A.lastPosition.room,A,oldApos,Publisher.EventType.Disappear);
                    B.firstPosition = A.firstPosition;
                    B.aliases.PushRange(A.aliases.ToArray());
                    B.aliases.Push(new Device.Alias(A.MAC, maxpoint));
                    foreach (String ssid in A.requestedSSIDs.Keys)
                        B.requestedSSIDs.TryAdd(ssid, 0);
                    deviceMap.remove(B.identifier);
                    foreach (Publisher pb in publishers)
                        if(pb.supportsOperation(Publisher.DisplayableType.Rename))
                            pb.publishRename(B.identifier, A.identifier);
                    B.identifier = A.identifier;
                    deviceMap.upsert(B.identifier,B,(d1,d2)=> { return d2; });
                    anoniDevices.Remove(A.MAC);
                    anoniDevices[B.MAC]= B.identifier;
                    
                }
            }
        }
       
        private bool isMACLocal(string sendingMAC)
        {
            long mac = Convert.ToInt64(sendingMAC, 16);
            return (mac & 0x020000000000) > 0;
        }
        private void locateAndPublish(Device d,Packet p)
        {
            Room oldRoom =(d.lastPosition!=null)?d.lastPosition.room:null;
            PositionTools.Position newposition = PositionTools.triangulate(p.Receivings);
            Publisher.EventType e = Publisher.EventType.Update;
            newposition.positionDate = p.Timestamp;
            if (oldRoom==null)
            {
                d.firstPosition = newposition;
                e = Publisher.EventType.Appear;
            }
            else if(oldRoom!=newposition.room)
            {
                PositionTools.Position oldpos = new PositionTools.Position(d.lastPosition);
                oldpos.positionDate = newposition.positionDate;
                placeInRoomAndPublish(oldRoom,d,oldpos, Publisher.EventType.MoveOut);
                e = Publisher.EventType.MoveIn;
            }
            d.lastPosition = newposition;
            placeInRoomAndPublish(d.lastPosition.room, d, newposition, e);

        }

        private void placeInRoomAndPublish(Room room, Device d, PositionTools.Position p, Publisher.EventType action)
        {
            //optionally here insert to update only if device has more than 5 minutes of history
            if (action == Publisher.EventType.Appear || action == Publisher.EventType.MoveIn)
                room.addDevice(d);
            else if (action != Publisher.EventType.Update)
                room.removeDevice(d);
            foreach (Publisher pb in publishers)
            {
                if(pb.supportsOperation(Publisher.DisplayableType.DeviceDevicePosition))
                    pb.publishPosition(d, p, action);
                if(pb.supportsOperation(Publisher.DisplayableType.SimpleStat))
                    pb.publishStat(room.devicecount, room, p.positionDate,Publisher.StatType.InstantaneousDeviceCount);
            }
        }

        public void kill()
        {
            killed = true;
            t.Cancel();
        }
        
        private void householdCleaning()
        {
            Device removed;
            while (deviceMap.popConditional((Device d)=>{ return d.lastPosition.positionDate.AddMinutes(5) < DateTime.Now; },(Device d)=> { return d.identifier; },out removed))
            {
                if (removed.anonymous)
                    anoniDevices.Remove(removed.MAC);
                removed.lastPosition = new PositionTools.Position(removed.lastPosition);
                removed.lastPosition.positionDate = DateTime.Now;
                placeInRoomAndPublish(removed.lastPosition.room,removed,removed.lastPosition,Publisher.EventType.Disappear);
            }
        }
    }
}
