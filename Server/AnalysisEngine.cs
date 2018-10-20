using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class AnalysisEngine
    {
        BlockingCollection<Packet> AnalysisQueue = new BlockingCollection<Packet>();
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
            AnalysisQueue.Add(p);
        }
    }
}
