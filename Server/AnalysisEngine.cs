using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class AnalysisEngine
    {
        ConcurrentQueue<Packet> AnalysisQueue = new ConcurrentQueue<Packet>();
        EventWaitHandle condVar = new EventWaitHandle(false, EventResetMode.AutoReset);
        ConcurrentDictionaryStack<String, Device> deviceMap = new ConcurrentDictionaryStack<String, Device>();
        ConcurrentDictionary<String, Byte> anoniDevices = new ConcurrentDictionary<string, byte>();

        volatile bool killed = false;
        int anPack = 0;
        /// <summary>
        /// Lanciare questo metodo ogni qual volta un pacchetto è "maturo" (ovvero ricevuto da tutte le stazioni o andato in timeout).
        /// </summary>
        /// <param name="p">Pacchetto da analizzare</param>
        /// <param name="c">Contesto</param>
        public void sendToAnalysisQueue(Packet p)
        {
            AnalysisQueue.Enqueue(p);
            condVar.Set();            
        }
        private void analyzerProcess()
        {
            while (!killed)
            {
                Packet p;
                bool success;
                try
                {
                    if (success = AnalysisQueue.TryDequeue(out p))
                    {
                        analyzePacket(p);
                        anPack++;
                    }
                    if (!success || (anPack & 1024) == 0)
                    {
                        updateLongTerm();
                        coalesceAnonymous();
                        anPack = 0;
                    }
                    if (!success)
                    {
                        condVar.WaitOne(3000);
                    }
                }
                catch (ObjectDisposedException)//fatal
                {
                    killed = true;
                }
            }
        }
        private void analyzePacket(Packet p)
        {
            Device d;
            bool anew;
            if (anew=!deviceMap.getKey(p.SendingMAC, out d))
            {
                d = new Device();
                d.MAC = p.SendingMAC;
                if(isMACLocal(p.SendingMAC))
                {
                    d.anonymous = true;
                    anoniDevices.TryAdd(p.SendingMAC, 0);
                }
                d.firstSeen = p.Timestamp;
            }
            d.lastSeen = p.Timestamp;
            d.dirty = true;
            if(p.RequestedSSID!=null)
            {
                d.requestedSSIDs.Add(p.RequestedSSID);
            }
            locate(d, p);
            if (anew)
            {
                deviceMap.upsert(d.MAC, d,(old,cur)=> { return cur; });//single thread safe only
            }
        }

        private void coalesceAnonymous()
        {
            //scansiona la tabella anonimous, provando a confrontare tutti gli elementi a due a due per verificare se possono essere lo stesso.
            //se c'è un pairing, elimina la vecchia identità da anoniDevices, 
            //abbi inoltre cura di unire i due oggetti in uno solo, salvato sotto il nuovo mac, ed elimina la entry con il vecchio MAC da mapDevices
            throw new NotImplementedException();
        }
       
        private bool isMACLocal(string sendingMAC)
        {
            throw new NotImplementedException();
        }
        private void locate(Device d,Packet p)
        {
            if (d.lastPositionSaving.AddSeconds(5).CompareTo(DateTime.Now) <= 0)
            {
                d.positionHistory.Push(d.lastPosition);
                d.lastPositionSaving = d.lastPosition.positionDate;
            }
            d.lastPositions.put(triangulate(p.Receivings));
            d.lastPosition = averagePosition(d.lastPositions);
        }

        private Position averagePosition(ConcurrentCircular<Position> lastPositions)
        {
            throw new NotImplementedException();
        }

        private Position triangulate(List<Packet.Reception> receivings)
        {
            throw new NotImplementedException();
        }
        public void kill()
        {
            killed = true;
            condVar.Dispose();
        }
        
        private void updateLongTerm()
        {
            //elimina da tabella deviceMap i vecchi dispositivi, avendo cura di eliminarlo anche da anoniDevices, e salvali nel db
            throw new NotImplementedException();
        }
    }
}
