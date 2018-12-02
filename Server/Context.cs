using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Context
    {
        Dictionary<String,Station> stations = new Dictionary<String,Station>();
        volatile bool inCalibration;
        AnalysisEngine analyzer = new AnalysisEngine();
        Calibrator calibrator = new Calibrator();
        Dictionary<int,int> numberOfAliveStationsPerRoom = new Dictionary<int,int>();
       
        public Analyzer getAnalyzer()
        {
            if (inCalibration)
                return calibrator;
            else
                return analyzer;
        }
        public void switchCalibration(bool calibrate)
        {
            inCalibration = calibrate;
        }
        
        public void addStation(Station s)
        {

        }
        public void getStation(String NameMAC)
        {

        }
        public void checkStationAliveness()
        {

        }


    }
}
