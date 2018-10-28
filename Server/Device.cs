using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;


namespace Server
{
    class Device
    {
        internal String MAC;
        internal DateTime firstSeen =DateTime.MinValue;
        internal DateTime lastSeen = DateTime.MinValue;
        internal ArrayList lastPositions = new ArrayList();//keep the positions of the last 30 seconds to average
        internal ConcurrentStack<Position> positions = new ConcurrentStack<Position>();
        internal HashSet<String> requestedSSIDs = new HashSet<string>();
        internal bool dirty = false;
    }
}
