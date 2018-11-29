using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Point
    {
        public double X;
        public double Y;
        public Point(double x,double y)
        {
            X = x;
            Y = y;
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
    }
}
