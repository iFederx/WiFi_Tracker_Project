using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime dtStart;
            DateTime dtEnd;
            TimeSpan tsDifference;

            dtStart = DateTime.Now;
            System.Threading.Thread.Sleep(100);
            dtEnd = DateTime.Now;
            tsDifference = dtEnd - dtStart;
            Console.WriteLine("CPU Ticks Count With Now : " + tsDifference.Ticks);

            long frequency = Stopwatch.Frequency;
            //Console.WriteLine("  Timer frequency in ticks per second = {0}", frequency);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            System.Threading.Thread.Sleep(100);
            sw.Stop();
            Console.WriteLine("CPU Ticks Count With StopWatch : " + sw.ElapsedTicks*10000000/frequency);
            Console.ReadLine();
        }
    }
}
