using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public abstract class Publisher
    {
        internal enum EventType  { New, Update, Removal };
        internal abstract void publish(Device d,EventType e);
    }
}
