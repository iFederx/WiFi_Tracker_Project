using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Panopticon
{
    class Tester
    {
        public double dist2RSSI(double dist)
        {
            if (dist < 1)
                return -25;
            return -30 * (Math.Log(dist, 5) + 1);
        }
        public double pos2dist(double x, double y, double sx, double sy)
        {
            System.Diagnostics.Debug.Print("Inserted with position " + x + " " + y);
            return Math.Sqrt((sx - x) * (sx - x) + (sy - y) * (sy - y));
        }
        Context ctx;
        Random rand;
        public Tester(Context _ctx)
        {
            ctx = _ctx;
            rand = new Random();
        }
        public void test()
        {
            Room r = ctx.getRoom("TestRoom");
            Station s1 = ctx.tryAddStation("0.0", null, false);
            Station s2 = ctx.tryAddStation("25.0", null, false);
            Station s3 = ctx.tryAddStation("0.25", null, false);
            Station s4 = ctx.createStation(r, "25.25", 25, 25, null);
            Station s5 = ctx.createStation(r, "12.12", 12, 12, null);
            Station s6 = ctx.createStation(r, "18.6", 18, 6, null);
            String[] macs = { "abc1", "abc2", "abc3", "abc4", "abc5" };
            String[] ssids = { "ssid1", "ssid2", "ssid3", "ssid4", "ssid5" };
            double toterror = 0;
            int nerrors = 0;
            while (true)
            {
                Packet p = new Packet(macs[rand.Next(0, 4)], ssids[rand.Next(0, 4)], DateTime.Now, "", "", 0);
                double x = 25 * rand.NextDouble();
                double y = 25 * rand.NextDouble();
                System.Diagnostics.Debug.Print("Inserted with real position " + x + " " + y);
                double xf=0, yf=0, xt=0, yt=0;
                xf = x + ((rand.NextDouble() * 2) - 1);
                yf = y + ((rand.NextDouble() * 2) - 1);
                xt += xf;
                yt += yf;
                p.received(s1, dist2RSSI(pos2dist(xf, yf, 0, 0)));
                xf = x + ((rand.NextDouble() * 2) - 1);
                yf = y + ((rand.NextDouble() * 2) - 1);
                xt += xf;
                yt += yf;
                p.received(s2, dist2RSSI(pos2dist(xf, yf, 25, 0)));
                xf = x + ((rand.NextDouble() * 2) - 1);
                yf = y + ((rand.NextDouble() * 2) - 1);
                xt += xf;
                yt += yf;
                p.received(s3, dist2RSSI(pos2dist(xf, yf, 0, 25)));
                /*xf = x + ((rand.NextDouble() * 2) - 1);
                yf = y + ((rand.NextDouble() * 2) - 1);
                xt += xf;
                yt += yf;
                p.received(s4, dist2RSSI(pos2dist(xf, yf, 25, 25)));
                xf = x + ((rand.NextDouble() * 2) - 1);
                yf = y + ((rand.NextDouble() * 2) - 1);
                xt += xf;
                yt += yf;
                p.received(s5, dist2RSSI(pos2dist(xf, yf, 12, 12)));
                xf = x + ((rand.NextDouble() * 2) - 1);
                yf = y + ((rand.NextDouble() * 2) - 1);
                xt += xf;
                yt += yf;
                p.received(s6, dist2RSSI(pos2dist(xf, yf, 18, 6)));*/
                p.testposition = new PositionTools.Position(x, y, r);
                xt /= p.Receivings.Count;
                yt /= p.Receivings.Count;
                System.Diagnostics.Debug.Print("Averaged: ", xt, " ", yt);
                double error = Math.Sqrt(Math.Pow((xt - x), 2) + Math.Pow((yt - y), 2));
                toterror += error;
                nerrors++;
                System.Diagnostics.Debug.Print("Averaged Error: " + error);
                System.Diagnostics.Debug.Print("Averaged Mean Error: " + toterror/nerrors);
                ctx.getAnalyzer().sendToAnalysisQueue(p);
                Thread.Sleep((int)(rand.NextDouble() * 1000));
            }
        }

		public void testFEDE()
		{
			/*StationAdder sa1 = new StationAdder();
			sa1.Show();
			*/

			Console.WriteLine("Programma avviato");
			//Connection.StartConnection();

			//Console.ReadLine();

			Thread socketListener = new Thread(new ThreadStart(Connection.StartConnection));
			socketListener.Start();
			/*
			double[] x = { dist2RSSI(0), dist2RSSI(10), dist2RSSI(20), dist2RSSI(30), dist2RSSI(40) };
			double[] y = { 0, 10, 20, 30, 40 };
			Context ctx = new Context();
			Thread backgroundProcessManager = new Thread(new ThreadStart(ctx.orchestrate));
			backgroundProcessManager.Start();

			//TODO FEDE: aprire finestra creazione stanza
			Room r = ctx.createRoom(new Room("TestRoom", 25, 25));
			StationHandler sh1 = null, sh2 = null, sh3 = null;
			//funzione che non so dove sarà chiamata
			//sh1 = new StationHandler(socket);
			Station s1 = ctx.createStation(r, "0.0", 0, 0, sh1);
			PositionTools.calibrateInterpolators(y, x, s1);
			Station s2 = ctx.createStation(r, "25.0", 25, 0, sh2);
			PositionTools.calibrateInterpolators(y, x, s2);
			Station s3 = ctx.createStation(r, "0.25", 0, 25, sh3);
			PositionTools.calibrateInterpolators(y, x, s3);

			//formazione di un oggetto Packet
			Packet p = new Packet("abcde928", "Alice33Test", DateTime.Now, "", "", 0);
			p.received(s1, dist2RSSI(12.5));
			p.received(s2, dist2RSSI(12.5));
			p.received(s3, dist2RSSI(Math.Sqrt(12.5 * 12.5 + 25 * 25)));
			ctx.getAnalyzer().sendToAnalysisQueue(p);

			Thread.Sleep(1500);

			p = new Packet("abcde928e", "Fastweb25Test", DateTime.Now, "", "", 0);
			p.received(s1, dist2RSSI(25));
			p.received(s2, dist2RSSI(Math.Sqrt(25 * 25 + 25 * 25)));
			p.received(s3, dist2RSSI(0));
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

			Thread.Sleep(8000);
			ctx.getAnalyzer().kill();
			*/
		}

	}
}
