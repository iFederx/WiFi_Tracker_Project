using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panopticon
{
    class PositionTools
    {
        private const double EXTERNAL_MARGIN = 3;
        public class Point
        {
            public double X;
            public double Y;
            public Point(double x, double y)
            {
                X = x;
                Y = y;
            }
            public Point(Point a)
            {
                X = a.X;
                Y = a.Y;
            }
            public Point Add(Point b)
            {
                return new Point(X + b.X, Y + b.Y);
            }
            public Point Subtract(Point b)
            {
                return new Point(X - b.X, Y - b.Y);
            }

            public double Module()
            {
                return Math.Sqrt(X * X + Y * Y);
            }

            public double Dot(Point b)
            {
                return X * b.X + Y * b.Y;
            }

            public Point MultiplyScalar(double scalar)
            {
                return new Point(X * scalar, Y * scalar);
            }
            public Point DivideScalar(double scalar)
            {
                return new Point(X / scalar, Y / scalar);
            }

            public Point Normalize(bool redirection)
            {
                double mod = this.Module();
                if (mod == 0)
                    return new Point(0, 0);
                if (X < 0 && redirection)
                    mod *= -1;
                return new Point(X / mod, Y / mod);
            }

            public void import(Point p)
            {
                this.X = p.X;
                this.Y = p.Y;
            }
        }

        public class Position : Point
        {
            internal double uncertainity;
            internal Room room;
            internal DateTime positionDate;
            public Position(double x, double y, Room r) : base(x, y)
            {
                room = r;
            }
            public Position(double x, double y, Room r,Double u) : base(x, y)
            {
                room = r;
                uncertainity=u;
            }

            public Position(Point a, Room r) : base(a)
            {
                room = r;
            }

            public Position(Point a,Room r, Double u):base(a)
            {
                room = r;
                uncertainity = u;
            }

            public Position(Position a) : base(a)
            {
                room = a.room;
                positionDate = a.positionDate;
                uncertainity = a.uncertainity;
            }
        }
        class Circle : Point
        {
            public double R;

            public Circle(double x, double y, double r) : base(x, y)
            {
                R = r;
            }
            public Circle(Point a, double r) : base(a)
            {
                R = r;
            }
        }
        internal static Position triangulate(List<Packet.Reception> receivings, DateTime receptiontime)
        {
            Position p = new Position(0, 0, receivings[0].ReceivingStation.location.room);
            p.positionDate = receptiontime;
            if (receivings.Count() > 2)
            {
                double vote = 0;
                double xavg = 0;
                double yavg = 0;
               foreach(Packet.Reception pr in receivings)
               {
                    double weight = Math.Pow(1 / (-pr.RSSI - 25), 3);
                    xavg += pr.ReceivingStation.location.X * weight;
                    xavg += pr.ReceivingStation.location.Y * weight;
                    vote += weight;
                }
                xavg /= vote;
                yavg /= vote;
                p.X = xavg;
                p.Y = yavg;
            }
            else
                p.uncertainity = double.MaxValue;
            return p;
        }
        
    }
}
