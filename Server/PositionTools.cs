using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panopticon
{
    class PositionTools
    {
        private static double[] stdDist = { normalizeDistance(0.25), normalizeDistance(1), normalizeDistance(4), normalizeDistance(8), normalizeDistance(16), normalizeDistance(32), normalizeDistance(64)};
        private static double[] stdRssi = { normalizeRSSI(-30), normalizeRSSI(-50), normalizeRSSI(-70), normalizeRSSI(-80), normalizeRSSI(-90), normalizeRSSI(-100), normalizeRSSI(-110)};
        internal static Interpolator StandardShortInterpolator = new Interpolators.MonotoneCubicHermite(stdRssi, stdDist);
        internal static Interpolator StandardLongInterpolator = new Interpolators.Lagrangian(stdRssi, stdDist);
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
            Position p = new Position(0, 0, receivings[0].ReceivingStation.location.room);
            p.positionDate = DateTime.Now;
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
                    p.import(triangulate(circles[ri[0]], circles[ri[1]], circles[ri[2]]));
                    safety++;
                }
                while (p == null && safety < 20);
                if (p == null)
                    p.import(new Point(0, 0));
                p=findBetterMinimum(circles, p);
            }
            else
                p.uncertainity = double.MaxValue;
            if(p.uncertainity!=double.MaxValue)
            {
                if (p.X < 0)
                    if (p.X > - EXTERNAL_MARGIN)
                        p.X = 0;
                    else
                        return new Position(0, 0, Room.externRoom);      
                else if (p.X > p.room.xlength)
                    if (p.X - p.room.xlength < EXTERNAL_MARGIN)
                        p.X = p.room.xlength;
                    else
                        return new Position(0, 0, Room.externRoom);
                if (p.Y < 0)
                    if(p.Y> - EXTERNAL_MARGIN)
                        p.Y = 0;
                    else
                        return new Position(0, 0, Room.externRoom);      
                else if (p.Y > p.room.ylength)
                    if (p.Y - p.room.ylength < EXTERNAL_MARGIN)
                        p.Y = p.room.ylength;
                    else
                        return new Position(0, 0, Room.externRoom);
            }
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
        private static double clusterize(Double measure)
        {
            return Math.Sign(measure) * Math.Min(2.5, Math.Abs(measure));
            //return Math.Sign(measure) * Math.Log(Math.Abs(measure) + 1, 3);
        }
        private static Position findBetterMinimum(Circle[] circles, Position p)
        {
            Point diff=new Point(100,100);
            Point pos = new Point(p);
            for (int i = 0; i < 30; i++)
            {
                diff = new Point(0, 0);
                for (int j = 0; j < circles.Length; j++)
                {
                    Point ediff = circles[j].Subtract(pos);
                    double d = ediff.Module() - circles[j].R;
                    diff = diff.Add(ediff.Normalize(false).MultiplyScalar(clusterize(d)));
                }
                pos = pos.Add(diff.MultiplyScalar(0.45));
                if (diff.Module() < 0.1)
                    break;
            }
            return new Position(pos,p.room,diff.Module());
        }

        private static double normalizeRSSI(double RSSI)
        {
            return -1.0 / RSSI;
        }
        private static double normalizeDistance(double denormalizedDistance)
        {
            return Math.Pow(denormalizedDistance,0.5);
        }
        private static double denormalizeDistance(double normalizedDistance)
        {
            return Math.Pow(normalizedDistance, 2);
        }
        private static double Rssi2Dis(Station receivingStation, double RSSI)
        {
            RSSI = normalizeRSSI(RSSI);
            double distance = receivingStation.shortInterpolator.calc(RSSI);
            if (double.IsNaN(distance))
                distance = receivingStation.longInterpolator.calc(RSSI);
            return denormalizeDistance(distance);//simulate ^4 power with 3 point interpolator
        }

        internal static void calibrateInterpolators(double[] dist, double[] rssi, Station s)
        {
            //normalize
            double[] distance=new double[dist.Length];
            double[] RSSI=new double[dist.Length];
            Array.Copy(dist, distance, dist.Length);
            Array.Copy(rssi, RSSI, dist.Length);
            for (int i=0;i<distance.Length;i++)
            {
                if (distance[i] == 0)
                    distance[i] = 0.1;
                distance[i] = normalizeDistance(distance[i]);
                RSSI[i] = normalizeRSSI(RSSI[i]);
            }
            Array.Sort(RSSI, distance);
            //check monotone
            for(int i=1;i<distance.Length;i++)
            {
                //normalized RSSI should be increasing -> distance should be decreasing
                if (distance[i] > distance[i - 1])
                    throw new Exception("Distances not monotonic");
            }
            s.shortInterpolator = new Interpolators.MonotoneCubicHermite(RSSI, distance);
            s.longInterpolator = new Interpolators.Lagrangian(RSSI, distance);
        }



        private static Point triangulate(Circle a, Circle b, Circle c)
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
            return new Point(a.Add(ex.MultiplyScalar(x)).Add(ey.MultiplyScalar(y)));

        }
    }
}
