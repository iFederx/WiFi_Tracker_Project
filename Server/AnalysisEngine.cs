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
        BlockingCollection<Packet> AnalysisQueue = new BlockingCollection<Packet>(new ConcurrentQueue<Packet>());
        ConcurrentDictionaryStack<String, Device> deviceMap = new ConcurrentDictionaryStack<String, Device>();
        ConcurrentDictionary<String, Byte> anoniDevices = new ConcurrentDictionary<string, byte>();
        CancellationTokenSource t = new CancellationTokenSource();
        List<Publisher> publishers;
        volatile bool killed = false;
        int anPack = 0;
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
                    anPack++;
                   
                    if (anPack == 64)
                    {
                        coalesceAnonymous();
                        householdCleaning();
                        anPack = 0;
                    }
                }
                catch (Exception)//fatal
                {
                    killed = true;
                }
            }
            foreach(Device dx in deviceMap.getAll())
                foreach (Publisher pb in publishers)
                    pb.publish(dx, Publisher.EventType.Removal);
        }
        private void analyzePacket(Packet p)
        {
            Device d;
            bool anew;
            if (anew=!deviceMap.getKey(p.SendingMAC, out d))
            {
                d = new Device();
                d.MAC = p.SendingMAC;
                d.identifier = d.MAC;
                if(isMACLocal(p.SendingMAC))
                {
                    d.anonymous = true;
                    d.identifier = "ANON-" +DateTime.Now.ToShortDateString()+"."+DateTime.Now.ToShortTimeString()+"-"+ d.MAC.GetHashCode().ToString("X").Substring(0,4);
                    anoniDevices.TryAdd(p.SendingMAC, 0);
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
            locate(d, p);
            if (anew)
            {
                deviceMap.upsert(d.MAC, d,(old,cur)=> { return cur; });//single thread safe only. To make it multithread i should also copy other fields
            }
            foreach (Publisher pb in publishers)
                pb.publish(d, anew ? Publisher.EventType.New : Publisher.EventType.Update);
        }

        private void coalesceAnonymous()
        {
            //scansiona la tabella anonimous, provando a confrontare tutti gli elementi a due a due per verificare se possono essere lo stesso.
            //se c'è un pairing, elimina la vecchia identità da anoniDevices, 
            //abbi inoltre cura di unire i due oggetti in uno solo, salvato sotto il nuovo mac ma con il vecchio identificativo, ed elimina la entry con il vecchio MAC da mapDevices
            throw new NotImplementedException();
        }
       
        private bool isMACLocal(string sendingMAC)
        {
            throw new NotImplementedException();
        }
        private void locate(Device d,Packet p)
        {
            d.lastPosition = PositionTools.triangulate(p.Receivings);
            d.lastPosition.positionDate = p.Timestamp;
            d.positionHistory.Push(d.lastPosition);
        }

             
        public void kill()
        {
            killed = true;
            t.Cancel();
        }
        
        private void householdCleaning()
        {
            Device removed;
            Byte b;
            while (deviceMap.popConditional((Device d)=>{ return d.lastSeen.AddMinutes(10) < DateTime.Now; },(Device d)=> { return d.MAC; },out removed))
            {
                if (removed.anonymous)
                    anoniDevices.TryRemove(removed.MAC, out b);
                foreach (Publisher p in publishers)
                    p.publish(removed, Publisher.EventType.Removal);
            }
        }
    }
}
