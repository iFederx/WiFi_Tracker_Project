using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class PositionTools
    {
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
        }
        public class Position : Point
        {
            internal double uncertainity;
            internal int Room;
            internal DateTime positionDate;
            public Position(double x, double y) : base(x, y)
            {
            }

            public Position(Point a) : base(a)
            {
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
        internal static Position triangulate(List<Packet.Reception> receivings)
        {
            Position p = new Position(Double.NaN, Double.NaN);
            if (receivings.Count() > 2)
            {
                Circle[] circles = new Circle[receivings.Count];
                for (int i = 0; i < circles.Length; i++)
                    circles[i] = new Circle(receivings[i].ReceivingStation.location, Rssi2Dis(receivings[i].ReceivingStation, receivings[i].RSSI));
                int safety = 0;
                int[] ri = { 0, 1, 1 };
                do
                {
                    ri = genCombinatorialIndex(ri, ri.Length - 1, circles.Length);
                    if (ri[0] == -1)
                    {
                        p = null;
                        break;
                    }
                    p = triangulate(circles[ri[0]], circles[ri[1]], circles[ri[2]]);
                    safety++;
                }
                while (p == null && safety < 20);
                if (p == null)
                    p = new Position(0, 0);
                p = findBetterMinimum(circles, p);
            }
            else
                p.uncertainity = double.MaxValue;
            p.Room = receivings[0].ReceivingStation.location.Room;
            p.positionDate = DateTime.Now;
            return p;
        }
        private static int[] genCombinatorialIndex(int[] pi, int i, int l)
        {
            pi[i]++;
            if (pi[i] >= l - (pi.Length - i - 1))
            {
                if (i == 0)
                    pi[i] = -1;
                else
                {
                    pi = genCombinatorialIndex(pi, i - 1, l);
                    pi[i] = pi[i - 1] + 1;
                }
            }
            return pi;
        }

        private static Position findBetterMinimum(Circle[] circles, Position p)
        {
            Point diff;
            Point pos = new Point(p);
            for (int i = 0; i < 20; i++)
            {
                diff = new Point(0, 0);
                for (int j = 0; j < circles.Length; j++)
                {
                    Point ediff = circles[j].Subtract(pos);
                    double d = ediff.Module() - circles[j].R;
                    diff = diff.Add(ediff.Normalize(false).MultiplyScalar(Math.Sign(d)*Math.Min(2.5,Math.Abs(d))));
                }
                pos = pos.Add(diff.MultiplyScalar(0.25));
                if (diff.Module() < 0.40)
                    break;
            }
            return new Position(pos);
        }

        internal static double normalizeRSSI(double RSSI)
        {
            return -1.0 / RSSI;
        }
        private static double Rssi2Dis(Station receivingStation, double RSSI)
        {
            RSSI = normalizeRSSI(RSSI);
            double distance = receivingStation.shortInterpolator.calc(RSSI);
            if (double.IsNaN(distance))
                distance = receivingStation.longInterpolator.calc(RSSI);
            return distance;
        }

        private static Position triangulate(Circle a, Circle b, Circle c)
        {

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
            return new Position(a.Add(ex.MultiplyScalar(x)).Add(ey.MultiplyScalar(y)));

        }
    }
}
