﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    interface Analyzer
    {
        void sendToAnalysisQueue(Packet p);
        void kill();
    }
}