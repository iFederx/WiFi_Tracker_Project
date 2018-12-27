using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace Server
{
    class Calibrator : Analyzer
    {
        private AnalysisEngine normalAnalyzer;
        BlockingCollection<Packet> AnalysisQueue = new BlockingCollection<Packet>(new ConcurrentQueue<Packet>());
        private PositionTools.Room RoomInCalibration = null;
        private Object locker=new Object();
        private volatile bool killed = false;
        CancellationTokenSource t;
        Publisher guiUpdater;
        DateTime PositionReady = DateTime.MaxValue;
        PositionTools.Point CalibratingPosition = null;
        Int16 Round = 0;
        
        internal bool inCalibration
        {
            get
            {
                lock (locker)
                {
                    return RoomInCalibration != null;
                }
            }
            
        }

        internal bool switchCalibration(PositionTools.Room toCalibrate)
        {
            bool ris;
            lock(locker)
            {
                if ((toCalibrate == null && RoomInCalibration == null) || (toCalibrate != null && RoomInCalibration != null))
                    ris =false;
                else
                {
                    RoomInCalibration = toCalibrate;
                    ris = true;
                    //TODO_FEDE: Open Calibration GUI, and attach to it guiUpdater
                }
            }
            return ris;

        }
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
            Packet p;
            t = new CancellationTokenSource(10000);
            while (!killed)
            {
                try
                {
                    p = AnalysisQueue.Take(t.Token);
                    //TODO_FEDE: 
                    //1: Determine the calibrating device: every time you detect a new device, send a notification through the guiUpdater publisher, to show it on screen.
                    //2: When on gui is selected a device, write it in a field in this class. Then change screen. 
                    //3: Ask on gui for next calibrating position. Write it here in CalibratingPosition. Then the user will press on a positionReady button. Then in that time, write in PositionReady the value DateTime.Now
                    //4: Ignore all the packet while PositionReady is null or the packets where the sendingMac is not the calibrating device mac or the timestamp is antecedent to PositionReady
                    //5: Collect 5 packets that have the said requisites. 
                    //6: Then, for every station st, calculate [st].dist[Round]=CalibratingPosition.subtract(st.location).Module(), [st].rssi[Round]=Average(rssi ricevuti dalla stazione st dai 5 pacchetti)
                    //7: Increment Round. If Round <4, goto to point #3
                    //8: Set PositionReady to DateTime.MaxValue, set Rount to zero;
                    //9: For every station st, call PositionTools.calibrateInterpolator([st].dist,[st].rssi,st). It may throw an exception if the calibration data makes no sense, so catch it and start again
                }
                catch (OperationCanceledException)
                {
                    t = new CancellationTokenSource(10000);
                }
            }
            while (AnalysisQueue.TryTake(out p)) ;
        }

        public void kill()
        {
            killed = true;
            if(t!=null)
            {
                try
                {
                    t.Cancel();
                }
                catch(Exception e)
                {}
            }
        }
    }
}
