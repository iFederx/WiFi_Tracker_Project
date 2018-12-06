using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;


namespace Server
{
    class Device
    {
        internal String identifier; //equal to MAC if not anonymous, a casual value else
        internal String MAC;
        internal DateTime firstSeen =DateTime.MinValue;
        internal DateTime lastSeen = DateTime.MinValue;
        /*keep the last positions to average (sliding weighted average up to when too old - 10 seconds ago-) save in lastPosition the last computed.
        If the position that was there was older than 5 seconds put it into positionHistory*/
        //internal ConcurrentCircular<PositionTools.Position> lastPositions = new ConcurrentCircular<PositionTools.Position>(16); //possibily deprecated: probe requests are not so frequent
        internal ConcurrentStack<PositionTools.Position> positionHistory = new ConcurrentStack<PositionTools.Position>();
        internal PositionTools.Position lastPosition;
        internal HashSet<String> requestedSSIDs = new HashSet<string>();
        internal bool anonymous = false;
        internal List<string> aliases = new List<string>();
    }
}
