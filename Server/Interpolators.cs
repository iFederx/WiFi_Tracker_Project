using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Panopticon
{
    public abstract class Interpolator
    {
        public Interpolator(double[] x, double[] y) { }
        protected internal Interpolator(){ }
        public abstract double calc(double x);
    }
    class Interpolators
    {
        public class Lagrangian : Interpolator
        {
            protected internal double[] interpolator;
            protected internal Lagrangian() { }
            public Lagrangian(double[] x, double[] y) : base(x, y)
            {
                interpolator = new double[x.Length];
                for (int i = 0; i < x.Length; i++)
                {
                    double[] temp = new double[x.Length];
                    temp[x.Length - 1] = 1;
                    double divider = 1;
                    for (int j = 0; j < x.Length; j++)
                    {
                        if (i == j)
                            continue;
                        divider *= (x[i] - x[j]);
                        for (int z = 0; z < x.Length; z++)
                        {
                            temp[z] = -x[j] * temp[z];
                            temp[z] += z < x.Length - 1 ? temp[z + 1] : 0;
                        }
                    }
                    for (int z = 0; z < x.Length; z++)
                        interpolator[z] += (y[i] * temp[z] / divider);
                }
            }

            public override double calc(double x)
            {
                double res = 0;
                for (int i = 0; i < interpolator.Length; i++)
                    res = res * x + interpolator[i];
                return res;
            }
        }

        public class MonotoneCubicHermite : Interpolator
        {
            protected internal double[] xp;
            protected internal double[] yp;
            protected internal double[] m;
            protected internal MonotoneCubicHermite() { }
            public MonotoneCubicHermite(double[] x, double[] y) : base(x, y)
            {
                double dpre=0;
                double dcurr=0;
                double alpha;
                double beta;
                double tau;
                xp = new double[x.Length];
                yp = new double[x.Length];
                m = new double[x.Length];
                for (int i=0; i < x.Length; i++)
                {
                    xp[i] = x[i];
                    yp[i] = y[i];
                    dpre = dcurr;
                    if (i < (x.Length - 1))
                        dcurr = (y[i + 1] - y[i]) / (x[i + 1] - x[i]);
                    if (i > 0 && Math.Sign(dcurr)==Math.Sign(dpre))
                        m[i] = (dcurr + dpre) / 2;
                    else
                        m[i] = 0;
                    if (dcurr == 0)
                        continue;
                    if(i>0&&dpre==0)
                    {
                        m[i] = 0;
                        m[i - 1] = 0;
                        continue;
                    }
                    if(i>0)
                    {
                        alpha = m[i - 1] / dpre;
                        beta = m[i] / dcurr;
                        if(!((alpha-Math.Pow(2*alpha+beta-3,2)/(3*(alpha+beta-2))>0)||((alpha+2*beta-3)<=0)||((2*alpha+beta-3)<=0)))
                        {
                            tau = 3 / Math.Sqrt(alpha * alpha + beta * beta);
                            m[i - 1] = tau * alpha * m[i - 1];
                            m[i] = tau * beta * m[i];
                        }
                    }
                }
            }
            public override double calc(double x)
            {
                double ymin=0;
                double xmin=0;
                double mmin = 0;
                double ymax = 0;
                double xmax = 0;
                double mmax = 0;
                if (x <= xp[0])
                    return double.NaN;
                if (x >= xp[xp.Length - 1])
                    return yp[xp.Length-1];
                int i = 0;
                for (i = 1; x > xp[i]; i++) ;
                xmax = xp[i];
                ymax = yp[i];
                mmax = m[i];
                xmin = xp[i + 1];
                ymin = yp[i+1];
                mmin = m[i+1];
                double h = (xmax - xmin);
                double t = (x - xmin) / h;
                return ymin * (2 * t * t * t - 3 * t * t + 1) + h * mmin * (t * t * t - 2 * t * t + t) + ymax * (-2 * t * t * t + 3 * t * t) + h * mmax * (t * t * t - t * t);
            }
        }
        private static void DoubleArrayToByteArray(byte[] ByteArr, int offset, double[] DoubleArr)
        {
            for (int j = 0; j < DoubleArr.Length; j++)
            {
                Array.Copy(BitConverter.GetBytes(DoubleArr[j]), 0, ByteArr, sizeof(double) * j + offset, sizeof(double));
            }
        }
        private static void ByteArrayToDoubleArray(double[] DoubleArr, int length, byte[] ByteArr, int offset)
        {
            for(int j=0;j<length;j++)
            {
                DoubleArr[j] = BitConverter.ToDouble(ByteArr, offset);
                offset += sizeof(double);
            }
        }
        public static byte[] serialize(Interpolator i)
        {
            byte[] ris;
            if (i is Lagrangian)
            {
                Lagrangian l = (Lagrangian)i;
                if (l.interpolator.Length > 255)
                    throw new InsufficientMemoryException();
                ris = new byte[2 + l.interpolator.Length * sizeof(double)];
                ris[0] = (byte)'L';
                ris[1] = (byte)l.interpolator.Length;
                DoubleArrayToByteArray(ris, 2, l.interpolator);
            }
            else if(i is MonotoneCubicHermite)
            {
                MonotoneCubicHermite m = (MonotoneCubicHermite)i;
                if (m.xp.Length > 255)
                    throw new InsufficientMemoryException();
                ris = new byte[2 + 3 * m.xp.Length * sizeof(double)];
                ris[0] = (byte)'M';
                ris[1] = (byte) m.xp.Length;
                DoubleArrayToByteArray(ris, 2, m.xp);
                DoubleArrayToByteArray(ris, 2 + m.xp.Length * sizeof(double), m.yp);
                DoubleArrayToByteArray(ris, 2 + 2 * m.xp.Length * sizeof(double), m.m);
            }
            else
                throw new NotSupportedException();
            return ris;
        }
        public static Interpolator deserialize(byte[] arr)
        {
            switch((char)arr[0])
            {
                case 'L':
                    Lagrangian ris = new Lagrangian();
                    ris.interpolator = new double[arr[1]];
                    ByteArrayToDoubleArray(ris.interpolator, ris.interpolator.Length, arr, 2);
                    return ris;
                case 'M':
                    MonotoneCubicHermite r = new MonotoneCubicHermite();
                    r.xp = new double[arr[1]];
                    r.yp = new double[arr[1]];
                    r.m = new double[arr[1]];
                    ByteArrayToDoubleArray(r.xp, r.xp.Length, arr, 2);
                    ByteArrayToDoubleArray(r.yp, r.xp.Length, arr, 2 + r.xp.Length*sizeof(double));
                    ByteArrayToDoubleArray(r.m, r.xp.Length, arr, 2 + 2 * r.xp.Length * sizeof(double));
                    return r;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
