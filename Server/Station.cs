using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Station
    {
        Position p;
        String NameMAC;
        DateTime lastHearthbeat;
        DateTime addedAt;
        public enum Status {Alive,Zombie};
        Status stat;
    }
}
