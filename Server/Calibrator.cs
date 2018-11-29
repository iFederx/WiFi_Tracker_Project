using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Calibrator : Analyzer
    {
        void Analyzer.sendToAnalysisQueue(Packet p)
        {
            throw new NotImplementedException();
        }
    }
}
