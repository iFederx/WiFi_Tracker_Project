using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace Server
{
    class Protocol
    {
        /// <summary>
        /// Interprets the command received through the socket           
        /// </summary>
        public static int Command(string text, Socket socket, int received)
        { 
            int x;
            if (text.IndexOf("REGISTER(") > -1)
            {
                int offset = text.IndexOf("REGISTER(");
                string macAddress = text.Substring(offset+9, 17);
                Console.WriteLine("An ESP board with MAC: " + macAddress + " has requested access");
                //TODO
                //verificare se esiste già la schedina
                //se esiste, sostituire il vecchio socket nella struttura
                //se NON esiste
                //aprire finestra di configurazione + calibrazione



                byte[] data = Encoding.UTF8.GetBytes("ACCEPT\r\n");
                socket.Send(data); //blocking method
                int result = ESP_SyncClock(socket);
                if (result == -1) return -1;

                Console.Write("Blinking... ");
                ESP_BlinkStart(socket);
                Thread.Sleep(40000);
                ESP_BlinkStop(socket);
                Console.Write("stop");
            }
            else if ((x = text.IndexOf("FILE")) > -1)
            {
                //leggere quanti byte aspettarsi
                //ciclo: se se ne sono ricevuti di meno, lanciare una receive
                //int offset = x + received
                byte[] data = Encoding.UTF8.GetBytes(text);
                string receivedPath = "./Received/"+"nomeSchedina"+"/";
                //bbbb
                int receivedBytesLen = socket.Receive(data, received, 2048, SocketFlags.None);
                received += receivedBytesLen;
                //tttt
                receivedBytesLen = socket.Receive(data, received, 2048, SocketFlags.None);
                received += receivedBytesLen;
                //file
                receivedBytesLen = socket.Receive(data, received, 2048, SocketFlags.None);
                received += receivedBytesLen;
                int fileNameLen = BitConverter.ToInt32(data, 0);
                string fileName = Encoding.ASCII.GetString(data, 4, fileNameLen) + ".txt";
                Console.WriteLine("Client:{0} connected & File {1} started received.", socket.RemoteEndPoint, fileName);
                BinaryWriter bWrite = new BinaryWriter(File.Open(receivedPath + fileName, FileMode.Append)); ;
                bWrite.Write(data, 4 + fileNameLen, received - 6 - 8);
                Console.WriteLine("File: {0} received & saved at path: {1}", fileName, receivedPath);



















            }

            return 0; //if here, all fine
        }

        /// <summary>
        /// Syncronizes clock of ESP board (Attention: it doesn't include SYNC message)
        /// </summary>
        public static int ESP_SyncClock(Socket socket)
        {
            Console.WriteLine("Starting CLOCK sync");
            int n=5; //number of PING-PONG repetitions
            int received, i;
            byte[] ping = Encoding.UTF8.GetBytes("PING\r\n");
            byte[] clock1 = Encoding.UTF8.GetBytes("CLOCK(");
            byte[] clock2 = Encoding.UTF8.GetBytes(")\r\n");
            byte[] recBuf = new byte[100];
            byte[] buffer = new byte[100];

            long frequency = Stopwatch.Frequency;
            Stopwatch sw = new Stopwatch();
            sw.Start(); //--------------------------------------START_TIMER
            for (i=0; i<n; i++)
            {
                socket.Send(ping); //blocking method
                Console.WriteLine("PING sent");

                received = socket.Receive(recBuf); //blocking method
                byte[] recMsg = new byte[received];
                Array.Copy(recBuf, recMsg, received);
                string text = Encoding.UTF8.GetString(recMsg);

                if (received == 0) return -1;
                //if (received > 0) Console.WriteLine("Received " + received + "B in this buffer: " + text);
                if (text.IndexOf("PONG") > -1)
                {
                    Console.WriteLine("Received PONG");
                    continue;
                }
                else break;
            }
            sw.Stop(); //--------------------------------------STOP_TIMER
            if (i < 5) return -1;
            long ticksElapsed = sw.ElapsedTicks * 10000000 / frequency;
            ticksElapsed = ticksElapsed / n / 2;
            DateTime ESP_Time = new DateTime(DateTime.Now.Ticks + ticksElapsed);
            //UE will be Unix Epoch version of the time
            TimeSpan clockUE = ESP_Time - new DateTime(1970, 1, 1);//new TimeSpan(ESP_Time.Ticks);
            uint timestamp = (uint) clockUE.TotalSeconds; //secondi totali dall'anno 0
            byte[] toSend = Encoding.UTF8.GetBytes("CLOCK("+timestamp.ToString()+")\r\n");
            //Console.WriteLine("Sending: " + Encoding.UTF8.GetString(toSend));
            if (socket.Send(toSend) > 0)
                Console.WriteLine("Clock sent to ESP board");

            return 0;
        }

        /// <summary>
        /// Starts blinking to localize ESP board
        /// </summary>
        public static void ESP_BlinkStart(Socket socket)
        {
            byte[] data = Encoding.UTF8.GetBytes("BLINK\r\n");
            socket.Send(data); //blocking method
        }

        /// <summary>
        /// Syncronizes clock of ESP board in any arbitrary time (SYNC version)
        /// </summary>
        public static void ESP_SyncClockRequest(Socket socket)
        {
            byte[] data = Encoding.UTF8.GetBytes("SYNC\r\n");
            socket.Send(data); //blocking method
            ESP_SyncClock(socket);
        }

        /// <summary>
        /// Wakes ESP board from stand-by state and starts sniffing again
        /// </summary>
        public static void ESP_BlinkStop(Socket socket)
        {
            byte[] data = Encoding.UTF8.GetBytes("OKLED\r\n");
            socket.Send(data); //blocking method
        }

        /// <summary>
        /// Puts ESP board in a stand-by state, i.e. stops sniffing
        /// </summary>
        public static void ESP_StandBy(Socket socket)
        {
            byte[] data = Encoding.UTF8.GetBytes("STANDBY\r\n");
            socket.Send(data); //blocking method
        }

        /// <summary>
        /// Syncronize clock of ESP board
        /// </summary>
        public static void ESP_Resume(Socket socket)
        {
            byte[] data = Encoding.UTF8.GetBytes("RESUME\r\n");
            socket.Send(data); //blocking method
        }

        /// <summary>
        /// Reboots ESP board
        /// </summary>
        public static void ESP_Reboot(Socket socket)
        {
            byte[] data = Encoding.UTF8.GetBytes("REBOOT\r\n");
            socket.Send(data); //blocking method
        }
    }
}
