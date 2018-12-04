using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Station
    {
        public enum Status { Alive, Zombie };
        
        internal PositionTools.Position location; //contains also Room info
        internal String NameMAC;
        internal DateTime lastHearthbeat;
        internal DateTime addedAt;
        public Interpolator primaryInterpolator;
        public Interpolator fallbackInterpolator;
        Status stat;
    }
}
