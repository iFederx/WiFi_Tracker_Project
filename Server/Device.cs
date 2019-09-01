using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Panopticon
{
    public class Device
    {
        internal class Alias
        {
            String MAC;
            int affidability;

            public Alias(string mac, int maxpoint)
            {
                MAC = mac;
                affidability = maxpoint;
            }
        }
        internal String identifier; //equal to MAC if not anonymous, a casual value else
        internal String MAC;
        /*keep the last positions to average (sliding weighted average up to when too old - 10 seconds ago-) save in lastPosition the last computed.
        If the position that was there was older than 5 seconds put it into positionHistory*/
        //internal ConcurrentCircular<PositionTools.Position> lastPositions = new ConcurrentCircular<PositionTools.Position>(16); //possibily deprecated: probe requests are not so frequent
        internal PositionTools.Position firstPosition;
        internal PositionTools.Position lastPosition;
        internal ConcurrentDictionary<String,byte> requestedSSIDs = new ConcurrentDictionary<string,byte>();
        internal bool anonymous = false;
        internal ConcurrentStack<Alias> aliases = new ConcurrentStack<Alias>();
        internal String HTCapabilities;

    }
}
