using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Server
{
    [Serializable()]
    [XmlInclude(typeof(Interpolators.Lagrangian))]
    [XmlInclude(typeof(Interpolators.MonotoneCubicHermite))]
    public abstract class Interpolator
    {
        public Interpolator(double[] x, double[] y) { }
        public abstract double calc(double x);
    }
    class Interpolators
    {
        public class Lagrangian : Interpolator
        {
            double[] interpolator;
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
            double[] xp;
            double[] yp;
            double[] m;
            Boolean reversedX = false;
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
                if (x[0] > x[1])
                    reversedX = true;



            }
            public override double calc(double x)
            {
                int i;
                int d;
                int d2;
                if(!reversedX)
                {
                    if (x < xp[0])
                        return yp[0];
                    if (x > xp[xp.Length - 1])
                        return double.NaN;
                    for (i = 1; x > xp[i]; i++) ;
                    d = -1;
                    d2 = 0;
                }
                else
                {
                    if (x > xp[0])
                        return yp[0];
                    if (x < xp[xp.Length - 1])
                        return double.NaN;
                    for (i = 1; x < xp[i]; i++) ;
                    d = 0;
                    d2 = -1;
                }
                double h = (xp[i+d2] - xp[i +d]);
                double t = (x - xp[i +d]) / h;
                return yp[i + d] * (2 * t * t * t - 3 * t * t + 1) + h * m[i + d] * (t * t * t - 2 * t * t + t) + yp[i+d2] * (-2 * t * t * t + 3 * t * t) + h * m[i+d2] * (t * t * t - t * t);
            }
        }

    }
}
