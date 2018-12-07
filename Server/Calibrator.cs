using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Calibrator : Analyzer
    {
        private AnalysisEngine analyzer;

        public Calibrator(AnalysisEngine analyzer)
        {
            this.analyzer = analyzer;
        }

        void Analyzer.sendToAnalysisQueue(Packet p)
        {
            //TODO
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
