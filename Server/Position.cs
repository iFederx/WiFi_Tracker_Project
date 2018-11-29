using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Position: Point
    {
        internal int uncertainity;
        internal int Room;
        internal DateTime positionDate;

        public Position(Point p)
        {
            this.X = p.X;
            this.Y = p.Y;
        }
    }
}
