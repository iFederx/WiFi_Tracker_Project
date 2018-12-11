using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Server
{
    class Calibrator : Analyzer
    {
        private AnalysisEngine normalAnalyzer;
        BlockingCollection<Packet> AnalysisQueue = new BlockingCollection<Packet>(new ConcurrentQueue<Packet>());
        private volatile PositionTools.Room RoomInCalibration = null;
        public Calibrator(AnalysisEngine nrmAzr)
        {
            normalAnalyzer = nrmAzr;
        }

        public void sendToAnalysisQueue(Packet p)
        {
            if (p.Receivings[0].ReceivingStation.location.room != RoomInCalibration)
                normalAnalyzer.sendToAnalysisQueue(p);
            else
                AnalysisQueue.Add(p);
        }

        internal void calibratorProcess()
        {
            //TODO
        }

        public void kill()
        {
            //TODO
        }
    }
}
