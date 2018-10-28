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
        AnalysisEngine analysEngine;
        Dictionary<int,int> numberOfAliveStationsPerRoom = new Dictionary<int,int>();

        public Context(AnalysisEngine anEngine)
        {
            analysEngine = anEngine;
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
