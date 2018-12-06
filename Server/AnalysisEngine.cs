using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class AnalysisEngine:Analyzer
    {
        ConcurrentDictionaryStack<String, Device> deviceMap; //id, corresponding device
        BlockingCollection<Packet> AnalysisQueue = new BlockingCollection<Packet>(new ConcurrentQueue<Packet>());
        Dictionary<String, String> anoniDevices = new Dictionary<string, string>(); //mac, corresponding id
        ConcurrentDictionary<String, ConcurrentDictionary<Device,Byte>> peoplePerRoom; 
        CancellationTokenSource t = new CancellationTokenSource();
        volatile bool killed = false;
        DateTime lastCleaning = DateTime.Now.AddSeconds(5);

        public AnalysisEngine(ConcurrentDictionaryStack<String, Device> DeviceMap, ConcurrentDictionary<String, ConcurrentDictionary<Device, Byte>> PeoplePerRoom)
        {
            deviceMap = DeviceMap;
            peoplePerRoom = PeoplePerRoom;
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
        private void analyzerProcess()
        {
            while (!killed)
            {
                Packet p;
                try
                {
                    p = AnalysisQueue.Take(t.Token);
                    analyzePacket(p);
                    if (lastCleaning.AddSeconds(2) < DateTime.Now)
                    {
                        coalesceAnonymous();
                        householdCleaning();
                        lastCleaning = DateTime.Now;
                    }
                }
                catch (Exception)//fatal
                {
                    killed = true;
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
                if(isMACLocal(p.SendingMAC))
                {
                    d.anonymous = true;
                    d.identifier = "ANON-" +DateTime.Now.ToShortDateString()+"."+DateTime.Now.ToShortTimeString()+"-"+ d.MAC.GetHashCode().ToString("X").Substring(0,4);
                    anoniDevices.Add(p.SendingMAC, d.identifier);
                }
                d.firstSeen = p.Timestamp;
            }
            if (p.RequestedSSID != null)
            {
                d.requestedSSIDs.Add(p.RequestedSSID);
            }
            if (d.lastSeen.AddSeconds(10) > p.Timestamp)
                return;
            d.lastSeen = p.Timestamp;
            d.dirty = Int32.MaxValue;
            locate(d, p);
            if (anew)
            {
                deviceMap.upsert(d.identifier, d,(old,cur)=> { return cur; });//single thread safe only. To make it multithread i should also copy other fields
            }
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
                    if(A.firstSeen>B.firstSeen)
                    {
                        second = A;
                        first = B;
                    }
                    else
                    {
                        first = A;
                        second = B;
                    }
                    if (first.lastSeen.AddMinutes(3) > DateTime.Now || first.lastSeen > second.firstSeen ||first.lastPosition.Room!=second.firstPosition.Room)
                        continue;
                    curpoint += Math.Max(0, 70 - (int)second.firstSeen.Subtract(first.lastSeen).TotalSeconds);
                    if (first.lastPosition == null || second.positionHistory.Count < 1)
                        continue;
                    curpoint += Math.Max(0, 40 - 7 * (int)first.lastPosition.Subtract(second.firstPosition).Module());
                    if (Math.Abs(Convert.ToInt64(first.MAC, 16) - Convert.ToInt64(second.MAC, 16)) < 2)
                        curpoint += 100;                    
                    if (curpoint>maxpoint)
                    {
                        maxpoint = Math.Min(100,curpoint);
                        bestMatch = B;
                    }
                }
                if(maxpoint>40)
                {
                    B = bestMatch;
                    if(A.firstSeen>bestMatch.firstSeen)
                    {
                        B = A;
                        A = bestMatch;
                    }
                    placeInRoom(A.lastPosition.Room, -1);
                    B.firstSeen = A.firstSeen;
                    B.aliases.AddRange(A.aliases);
                    B.aliases.Add(new Device.Alias(A.MAC, maxpoint));
                    B.requestedSSIDs.UnionWith(B.requestedSSIDs);
                    A.positionHistory.PushRange(B.positionHistory.ToArray());
                    B.positionHistory = A.positionHistory;
                    deviceMap.remove(B.identifier);
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
        private void locate(Device d,Packet p)
        {
            PositionTools.Position newPosition = PositionTools.triangulate(p.Receivings);
            if(d.lastPosition==null)
            {
                placeInRoom(newPosition.Room, 1);
                d.firstPosition = newPosition;
            }
            else if(d.lastPosition.Room!=newPosition.Room)
            {
                placeInRoom(d.lastPosition.Room, -1);
                placeInRoom(newPosition.Room, 1);
            }
            d.lastPosition = newPosition;
            d.lastPosition.positionDate = p.Timestamp;
            d.positionHistory.Push(d.lastPosition);
        }

        private void placeInRoom(String room, int v)
        {
            //optionally here insert to update only if device has more than 5 minutes of history
            int val = 0;
            if (!peoplePerRoom.TryGetValue(room, out val))
                val = 0;
            val += v;
            peoplePerRoom[room] = val;
        }

        public void kill()
        {
            killed = true;
            t.Cancel();
        }
        
        private void householdCleaning()
        {
            Device removed;
            while (deviceMap.popConditional((Device d)=>{ return d.lastSeen.AddMinutes(5) < DateTime.Now; },(Device d)=> { return d.identifier; },out removed))
            {
                if (removed.anonymous)
                    anoniDevices.Remove(removed.MAC);
                placeInRoom(removed.lastPosition.Room, -1);
            }
        }
    }
}
