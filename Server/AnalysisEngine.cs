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
        Dictionary<String, Device> deviceMap = new Dictionary<string, Device>();
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
        private void analyzePacket()
        {
            Packet p;
            if(AnalysisQueue.TryDequeue(out p))
            {

            }
            return;
        }
    }
}
