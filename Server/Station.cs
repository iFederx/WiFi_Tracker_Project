﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Station
    {
        internal PositionTools.Position location; //contains also Room info
        internal String NameMAC;
        internal DateTime lastHearthbeat;
        internal DateTime addedAt;
        public enum Status {Alive,Zombie};
        Status stat;
    }
}
