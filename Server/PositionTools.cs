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
            Point accumulator = new Point(0, 0);
            p.positionDate = receptiontime;
            if (receivings.Count() > 2)
            {
                double minrssi = 0;
                double avgrssi = 0;
                double vote = 0;
                foreach(Packet.Reception pr in receivings)
                {
                    minrssi = pr.RSSI < minrssi ? pr.RSSI : minrssi;
                    avgrssi += pr.RSSI;
                    double weight = Math.Pow(1 / (-(pr.RSSI>26?pr.RSSI:26) - 25), 3);
                    accumulator = accumulator.Add(pr.ReceivingStation.location.MultiplyScalar(weight));
                    vote += weight;
                }
                avgrssi /= receivings.Count();
                accumulator = accumulator.DivideScalar(vote);
                if (avgrssi < -90 || (avgrssi < -70 && minrssi < -55))
                    p = new Position(0, 0, Room.externRoom);
                else
                {
                    if (receivings.Count() == 3)
                    {
                        Point tr = triangulate_circles(receivings);
                        if (tr != null)
                            accumulator = accumulator.MultiplyScalar(0.8).Add(tr.MultiplyScalar(0.2));
                    }
                    p.import(accumulator);
                }
            }
            else
                p.uncertainity = double.MaxValue;
            return p;
        }
        private static double rssi2dis(double rssi)
        {
            rssi = -rssi - 30;
            rssi = rssi > 0 ? rssi : 0;
            double dist = Math.Pow(2, rssi / 8) - 1;
            return dist;
        }
        private static Point triangulate_circles(List<Packet.Reception> receivings)
        {
            Circle a = new Circle(receivings[0].ReceivingStation.location, rssi2dis(receivings[0].RSSI));
            Circle b = new Circle(receivings[1].ReceivingStation.location, rssi2dis(receivings[1].RSSI));
            Circle c = new Circle(receivings[2].ReceivingStation.location, rssi2dis(receivings[2].RSSI));
            Point col1 = a.Subtract(b).Normalize(true);
            Point col2 = b.Subtract(c).Normalize(true);
            if (col1.Subtract(col2).Module() < 0.1)
            {
                return null;
            }
            Point ex = b.Subtract(a).Normalize(false);
            double i = ex.Dot(c.Subtract(a));
            Point ey = (c.Subtract(a).Subtract(ex.MultiplyScalar(i))).Normalize(false);
            double d = b.Subtract(a).Module();
            double j = ey.Dot(c.Subtract(a));
            double x = (a.R * a.R - b.R * b.R + d * d) / (2 * d);
            double y = (a.R * a.R - c.R * c.R + i * i + j * j) / (2 * j) - i * x / j;
            return new Point(a.Add(ex.MultiplyScalar(x)).Add(ey.MultiplyScalar(y)));
        }
    }
}
