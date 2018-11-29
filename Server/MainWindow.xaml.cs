using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static double c(double[] p,object p2)
        {
            double[][] p2x=(double[][])p2;
            double sum = 0;
            double sumsum = 0;
            for(int i=0;i<p2x.Length;i++)
            {
                sumsum = 0;
                for (int j = 0; j < p.Length; j++)
                {
                    sumsum += (p2x[i][j] * p[j]);
                }
                sumsum -= (p2x[i][p.Length]);
                sum += Math.Pow(sumsum, 2);
            }
            return sum;

        }
      
        public MainWindow()
        {
            InitializeComponent();
            double[] pt = new double[5];
            double[][] pt2 = new double[5][];
            for(int i=0;i<pt2.Length;i++)
            {
                pt2[i] = new double[6];
                for(int j=0;j<5;j++)
                {
                    pt2[i][j] = Math.Pow(2*i, 5-j-1);
                }
                pt2[i][5] = 4*i*i;

            }
            double[] ris = GrandientDescender.minimize(c, 1000, pt2,pt,0.000000001);
            double err = Math.Sqrt(c(ris,pt2));
            int a = 1;
            a++;
        }
    }
}
