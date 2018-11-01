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
                        anPack = 0;
                    }
                    if (!success)
                    {
                        condVar.WaitOne();
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
            //check aliases of anonymous, and if corresponds set p.sendingMAC all'alias con cui è registrato
            if (anew=!deviceMap.getKey(p.SendingMAC, out d))
            {
                d = new Device();
                d.MAC = p.SendingMAC;
                if(isMACLocal(p.SendingMAC))
                {
                    d.anonymous = true;
                }
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
                if (d.anonymous)
                    d = coalesce(d);
                deviceMap.upsert(d.MAC, d,(old,cur)=> { return cur; });//single thread safe only
            }
        }

        private Device coalesce(Device d)
        {
            //try to see if the Device has a previous version of itself, and return the old version, adding the new fields and the new mac to the aliases
            //add alias to the table
            //if impossible to fuse, generate
            throw new NotImplementedException();
        }

        private bool isMACLocal(string sendingMAC)
        {
            throw new NotImplementedException();
        }

        private void triangulate(Device d,Packet p)
        { }
        public void kill()
        {
            killed = true;
            condVar.Dispose();
        }
        
        private void updateLongTerm()
        {

        }
    }
}
