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
      
        public MainWindow()
        {
            InitializeComponent();
            /*List<Packet.Reception> recv = new List<Packet.Reception>();
            Station s1;
            Packet.Reception r1;
            s1 = new Station();
            r1 = new Packet.Reception();
            r1.ReceivingStation = s1;
            s1.location = new PositionTools.Position(0, 0);
            r1.RSSI = 2;
            recv.Add(r1);
            s1 = new Station();
            r1 = new Packet.Reception();
            r1.ReceivingStation = s1;
            s1.location = new PositionTools.Position(3, 0);
            r1.RSSI = 2;
            recv.Add(r1);
            s1 = new Station();
            r1 = new Packet.Reception();
            r1.ReceivingStation = s1;
            s1.location = new PositionTools.Position(1.5, 1);
            r1.RSSI = 3;
            recv.Add(r1);
            /*s1 = new Station();
            r1 = new Packet.Reception();
            r1.ReceivingStation = s1;
            s1.location = new PositionTools.Position(0,0);
            r1.RSSI =1;
            recv.Add(r1);

            s1 = new Station();
            r1 = new Packet.Reception();
            r1.ReceivingStation = s1;
            s1.location = new PositionTools.Position(4, 4);
            r1.RSSI = 2;
            recv.Add(r1);

            s1 = new Station();
            r1 = new Packet.Reception();
            r1.ReceivingStation = s1;
            s1.location = new PositionTools.Position(2, 0);
            r1.RSSI = 1.08;
            recv.Add(r1);

            s1 = new Station();
            r1 = new Packet.Reception();
            r1.ReceivingStation = s1;
            s1.location = new PositionTools.Position(0, 2);
            r1.RSSI = 1.08;
            recv.Add(r1);*/

            //PositionTools.Position pz=PositionTools.triangulate(recv);
            double[] x = { 3.3, 2.5, 1.6, 1.1 };
            double[] y = { 0, 1, 5, 20 };
            Interpolator i =new Interpolators.Lagrangian(x, y);
            
            int z = 0;
            z++;
        }
    }
}
