using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        public static double cost(double[] p,object p2)
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
      
        public double dist2RSSI(double dist)
        {
            if (dist < 1)
                return -30;
            return -30 * (Math.Log(dist, 5) + 1);
        }
        public MainWindow()
        {
            InitializeComponent();
            
            double[] x = { dist2RSSI(0), dist2RSSI(10), dist2RSSI(20), dist2RSSI(30), dist2RSSI(40)};
            double[] y = { 0, 10,20, 30, 40 };
            Context ctx = new Context();
            Thread backgroundProcessManager = new Thread(new ThreadStart(ctx.orchestrate));
            backgroundProcessManager.Start();
            PositionTools.Room r=ctx.createRoom("TestRoom", 25, 25);
            StationHandler sh1=null,sh2=null,sh3 = null;
            Station s1=ctx.createStation(r, "0.0", 0, 0,sh1);
            PositionTools.calibrateInterpolators(y, x, s1);
            Station s2 = ctx.createStation(r, "25.0", 25, 0,sh2);
            PositionTools.calibrateInterpolators(y, x, s2);
            Station s3 = ctx.createStation(r, "0.25", 0, 25,sh3);
            PositionTools.calibrateInterpolators(y, x, s3);
          
            Packet p = new Packet("abcde928", "Alice33Test",DateTime.Now,"","",0);
            p.received(s1, dist2RSSI(12.5));
            p.received(s2, dist2RSSI(12.5));
            p.received(s3, dist2RSSI(Math.Sqrt(12.5 * 12.5 + 25 * 25)));
            ctx.getAnalyzer().sendToAnalysisQueue(p);

            Thread.Sleep(12000);

            //ctx.switchCalibration(true, r);

            p = new Packet("abcde928", "Fastweb25Test", DateTime.Now, "", "", 0);
            p.received(s1, dist2RSSI(25));
            p.received(s2, dist2RSSI(Math.Sqrt(25 * 25 + 25 * 25)));
            p.received(s3, dist2RSSI(0));
            ctx.getAnalyzer().sendToAnalysisQueue(p);

            //ctx.switchCalibration(false,r);

            p = new Packet("abcde929", "Polito", DateTime.Now, "", "", 0);
            p.received(s1, dist2RSSI(0));
            p.received(s2, dist2RSSI(25));
            p.received(s3, dist2RSSI(25));
            ctx.getAnalyzer().sendToAnalysisQueue(p);

            Thread.Sleep(2000);
            ctx.getAnalyzer().kill();
        }
    }
}
