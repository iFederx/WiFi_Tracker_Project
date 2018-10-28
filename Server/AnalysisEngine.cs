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
        InsertionSortedConcurrentDictionary<String, Device> deviceMap = new InsertionSortedConcurrentDictionary<String, Device>();
        volatile bool killed = false;
        int anPack = 0;
        public class AnalysisResult
        {

        }
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
        private void analyzePacket(Packet p)
        {
            Device d;
            bool anew;
            if (anew=!deviceMap.getKey(p.SendingMAC, out d))
            {
                d = new Device();
                d.MAC = p.SendingMAC;
                d.firstSeen = p.Timestamp;
            }
            d.lastSeen = p.Timestamp;
            d.dirty = true;
            if(p.RequestedSSID!=null)
            {
                d.requestedSSIDs.Add(p.RequestedSSID);
            }
            triangulate(d, p);
            if (anew)
            {
                deviceMap.update(d.MAC, d);
            }
        }
        private void triangulate(Device d,Packet p)
        { }
        public void kill()
        {
            killed = true;
            condVar.Dispose();
        }
        private void analyzerProcess()
        {
            while(!killed)
            {
                Packet p;
                bool success;
                try
                {
                    if (success=AnalysisQueue.TryDequeue(out p))
                    {
                        analyzePacket(p);
                        anPack++;
                    }
                    if(!success||(anPack&1024)==0)
                    {
                        updateLongTerm();
                        anPack = 0;
                    }
                    if(!success)
                    {
                        condVar.WaitOne();
                    }
                }
               catch(ObjectDisposedException)//fatal
                {
                    killed = true;
                }
            }           
        }
        private void updateLongTerm()
        {

        }
    }
}
