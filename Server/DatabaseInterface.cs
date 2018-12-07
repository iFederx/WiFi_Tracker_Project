using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class DatabaseInterface:Publisher
    {
        public DatabaseInterface()
        {
            //TODO
        }

        internal void databaseProcess()
        {
            //TODO
        }

        internal void kill()
        {
            //TODO
        }

        internal override void publishPosition(Device d, EventType e)
        {
            //TODO
        }

        internal override void publishRename(string oldId, string newId)
        {
            //TODO
        }

        internal override void publishStat(double stat, PositionTools.Room r, StatType s)
        {
            //TODO
        }
    }
}
