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
        public Context(AnalysisEngine anEngine)
        {
            analysEngine = anEngine;
        }
        ArrayList numberOfAliveStationsPerRoom = new ArrayList();
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
