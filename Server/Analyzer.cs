using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panopticon
{
    interface Analyzer
    {
        void sendToAnalysisQueue(Packet p);
        void kill();
    }
}
