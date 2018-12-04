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
            public MonotoneCubicHermite(double[] x, double[] y) : base(x, y)
            {
            }
            public override double calc(double x)
            {
                //https://en.wikipedia.org/wiki/Monotone_cubic_interpolation
                //https://en.wikipedia.org/wiki/Cubic_Hermite_spline
                throw new NotImplementedException();
            }
        }
    }
}
