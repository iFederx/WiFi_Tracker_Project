﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Windows;

namespace Panopticon
{
    class Protocol
    {
		public delegate void SafeNewStation(string _string, Socket socket);

		private const int BUFFER_SIZE = 2048;
		static readonly Dictionary<Socket, string> macAddresses = new Dictionary<Socket,string>(); //only in this case, in the MAC addresses there aren't semi-columns (:)
        /// <summary>
        /// Interprets the command received through the socket           
        /// </summary>
        public static int Command(string text, Socket socket, int received)
        {
            int x;
            int toReceive = 0;
            int fileSize = 0;
            int timestamp = 0;
			DateTime timestampDT = new DateTime();

			if (text.IndexOf("REGISTER(") > -1)
            {
                int offset = text.IndexOf("REGISTER(");//----------------------------------------------------------
                string macAddress = text.Substring(offset+9, 17);
				macAddresses.Add(socket, macAddress.Replace(@":", string.Empty));
                Console.WriteLine("An ESP board with MAC: " + macAddress + " has requested access");
				//TODO
				//verificare se esiste già la schedina
				//se esiste, sostituire il vecchio socket nella struttura
				//se NON esiste
				//aprire finestra di configurazione

				//apro finestra configurazione nuova scheda
				var d = new SafeNewStation(GuiInterface.statlinkedwindow.NewStation);
				Application.Current.Dispatcher.Invoke(d, macAddress.Replace(@":", string.Empty), socket);

				byte[] data = Encoding.UTF8.GetBytes("ACCEPT\r\n");
                socket.Send(data); //blocking method
				
                int result = ESP_SyncClock(socket);
                if (result == -1) return -1;
            }
            else if ((x = text.IndexOf("FILE")) > -1)
            {
                //FILE\r\n BBBB TTTT 010101010101010101010 
                
                byte[] buffer = Encoding.UTF8.GetBytes(text);
                byte[] data = new byte[BUFFER_SIZE];
                //copio l'array "data" nel buffer di ricezione
                for (int i=0; i<received; i++)
                {
                    data[i] = buffer[i];
                }

                while (received < 14)
                {
                    //ricevo di nuovo, finché non ricevo tutti i metadati
                    int receivedBytesLen = socket.Receive(data, received, BUFFER_SIZE - received, SocketFlags.None);
                    received += receivedBytesLen;
                }

                //ora ho ricevuto i metadati
                if (received > 14)
                {
                    //leggere quanti byte si devono ricevere e timestamp
                    //System.Net.IPAddress.NetworkToHostOrder(data);
                    byte[] fileSizeBytes = new byte[4];
                    byte[] timestampBytes = new byte[4];
                    byte[] timestampBytesReversed = new byte[4];

                    for (int i = 0; i < 4; i++)
                    {
                        fileSizeBytes[3-i] = data[i+6];
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        timestampBytes[3-i] = data[i + 10];
                    }
                    
                    fileSize = BitConverter.ToInt32(fileSizeBytes, 0);
                    timestamp = BitConverter.ToInt32(timestampBytes, 0);
					timestampDT = FileParser.TimeFromUnixTimestamp(timestamp);
                    toReceive = fileSize - received + 14;
				}

				//creazione file di ricezione
				string mac = macAddresses[socket];
				if (mac == null) mac = "other";
                string receivingFolderPath = "./Received/" + mac + "/";
				FileParser.CheckFolder(receivingFolderPath);
				
                //string fileName = "prova.txt";
				string timeString = timestampDT.ToString("yyyyMMdd_HHmmss");
				string fileName = timeString + ".txt";
                string filePath = receivingFolderPath + fileName;
                FileStream fs = File.Open(filePath, FileMode.Create);

                //DONE: capire se 1) si devono ricevere altri byte o 2) si ha già tutto
                //scrivo quello che ho ricevuto
                fs.Write(data, 14, data.Length - 14); //13 perché salto "FILE\r\n BBBB TTTT"
                
                while (toReceive > 0)
                {
                    //ricevo ancora
                    Array.Clear(data, 0, BUFFER_SIZE); //svuoto buffer ricezione
                    int receivedBytesLen = socket.Receive(data, 0, BUFFER_SIZE, SocketFlags.None);
                    received = receivedBytesLen;
                    toReceive = toReceive - received;
                    //scrivo
                    fs.Write(data, 0, received);
                    
                }
                
                fs.Close(); //chiudo il file
                
                Console.WriteLine("Client:{0} connected & File {1} started received.", socket.RemoteEndPoint, fileName);

				//TODO: sarà spostato
				//Dictionary<String, Packet> packets = FileParser.Parse(@filePath, new Station()); //TODO: Station assente
				//TODO: spedire lista packets a qualcuno


            }

            return 0; //if here, all fine
        }

		/// <summary>
		/// Syncronizes clock of ESP board (Attention: it doesn't include SYNC message).
		/// After this phase, ESP board will begin the sniffing
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
            TimeSpan clockUE = ESP_Time - new DateTime(1970, 1, 1); //new TimeSpan(ESP_Time.Ticks);
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
			Console.Write("Blinking... ");
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
			Console.WriteLine("Blinking stopped");
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
