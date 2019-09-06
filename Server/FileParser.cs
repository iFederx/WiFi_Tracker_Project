using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace Panopticon
{
    class FileParser
    {
		internal Context ctx;
		private List<FileSystemWatcher> watchers;
		ConcurrentDictionary<string, MetaPacket> metaPackets; //the key is the hash
		volatile bool killed = false;
		int sleepTime = 60000;
		volatile int stationGC = 0;

		public FileParser(Context _ctx)
		{
			ctx = _ctx;
			watchers = new List<FileSystemWatcher>();
			metaPackets = new ConcurrentDictionary<string, MetaPacket>();
		}

		public struct MetaPacket
		{
			public Packet packet;
			public DateTime queueInsertionTime;
			public Room room;

			public MetaPacket(Packet _tempPacket, DateTime _now, Room _room) : this()
			{
				packet = _tempPacket;
				queueInsertionTime = _now;
				room = _room;
			}
		}
		public void kill()
		{
			killed = true;
		}

		internal void AddWatcher(string _mac) //metodo da chiamare per ogni Station creata
		{
			var watcher = new FileSystemWatcher();
			watcher.Path = "./Received/" + _mac + "/";
			watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
			watcher.Filter = "*.txt";
			watcher.Changed += new FileSystemEventHandler(OnChanged);
			watcher.EnableRaisingEvents = true;
			watchers.Add(watcher);
		}

		private void OnChanged(object source, FileSystemEventArgs e)
		{
			Console.WriteLine(e.ChangeType + " file: " + @e.FullPath);
			string[] directories = e.FullPath.Split('/');
			int folderCount = directories.Length;
			//TODO: chiudere connessione con station se la station non è presente in stations
			if (ctx.StationConfigured(directories[2])) //true se la station è presente in stations
			{
				Station s = ctx.getStation(directories[2]);
				Parse(e.FullPath, s);
			}
		}

		internal void packetizerProcess()
		{
			/*
			 * processo che legge packets ogni minuto e:
			 * - manda in analisi i packet maturi, rimuovendoli dalla struttura dati
			 * (maturo: con un numero di ricezioni uguale al numero di station nella stanza (ma almeno 2)
			 * - elimina i packet non maturi più vecchi di 5/10 minuti
			 */

			//per ogni unità nella coda ordinata
			while (!killed)
			{
				Thread.Sleep(sleepTime);
				stationGC++;
				stationGC %= 2; //ogni quanti minuti chiamo checkStationAliveness()
				foreach (MetaPacket metapak in metaPackets.Values)
				{
					if ((metapak.queueInsertionTime - DateTime.Now).TotalMinutes > 10)
					{
						//getto via il metapaket
						MetaPacket trash;
						metaPackets.TryRemove(metapak.packet.Hash, out trash);

					}
					else if (metapak.room.stationcount == metapak.packet.Receivings.Count)
					{
						//se sono qui, il pacchetto è "maturo"
						ctx.getAnalyzer().sendToAnalysisQueue(metapak.packet);
						//lo rimuovo dalla coda
						MetaPacket trash;
						metaPackets.TryRemove(metapak.packet.Hash, out trash);
					}
				}
				if (stationGC == 1)
					foreach (Room r in ctx.getRooms())
						ctx.checkStationAliveness(r);
			}

		}

		/// <summary>
		/// This method reads an ESP input file and returns a List of Packet objects
		/// </summary>
		public void Parse(String filePath, Station receivingStation)
        {
            string line;
            StreamReader file = new StreamReader(@filePath);
            Dictionary<String,Packet> packets = new Dictionary<string, Packet>(); //the key is the hash
            while ((line = file.ReadLine()) != null)
            {
                string Type = "", SubType = "", RSSI = "", SRC = "", seq_num = "", TIME = "", HASH = "", SSID_id = "", SSID_lenght = "", SSID = "", HT_id = "", HT_cap_len = "", HT_cap_str = "";
                var fields = line.Split(',');
                
                foreach (string field in fields) //each cycle reads a line
                {
                    var fieldSplit = field.Split('=');
                    string fieldLabel = fieldSplit[0];
                    switch (fieldLabel)
                    {
                        case "Type":
                            Type = fieldSplit[1];
                            break;

                        case " SubType":
                            SubType = fieldSplit[1];
                            break;

                        case " RSSI":
                            RSSI = fieldSplit[1];
                            break;

                        case " SRC":
                            SRC = fieldSplit[1].Replace(@":", string.Empty);
                            break;

                        case " seq_num":
                            seq_num = fieldSplit[1];
                            break;

                        case " TIME":
                            TIME = fieldSplit[1];
                            break;

                        case " HASH":
                            HASH = fieldSplit[1];
                            break;

                        case " SSID_id":
                            SSID_id = fieldSplit[1];
                            break;

                        case " SSID_lenght":
                            SSID_lenght = fieldSplit[1];
                            break;

                        case " SSID":
                            SSID = fieldSplit[1];
                            break;

                        case " HT_id":
                            HT_id = fieldSplit[1];
                            break;

                        case " HT_cap_len":
                            HT_cap_len = fieldSplit[1];
                            break;

                        case " HT_cap_str":
                            HT_cap_str = fieldSplit[1];
                            break;
                    }
                }
                
                if (HASH!="" && HASH.Length<40 && RSSI!="" && TIME!="")
                {
					if (!packets.ContainsKey(HASH))
					{
						Packet packet = new Packet(SRC, SSID, TimeFromUnixTimestamp(int.Parse(TIME)), HASH, HT_cap_str, long.Parse(seq_num));
						packet.received(receivingStation, double.Parse(RSSI));
						packets.Add(HASH, packet);
					}
                }
            }
			file.Close();

			//aggiungo i dati del file appena letto, alla struttura thread-safe
			foreach (Packet pak in packets.Values)
			{
				string hash = pak.Hash;
				if (metaPackets.ContainsKey(hash))
				{
					metaPackets[hash].packet.received(receivingStation, pak.Receivings.First<Packet.Reception>().RSSI);
				}
				else
				{
					var mp = new MetaPacket(pak, DateTime.Now, receivingStation.location.room);
					metaPackets.TryAdd(hash, mp);
				}
			}	
        }

		internal static void CheckFolder(string receivingFolderPath)
		{
			//crea cartella, se non esiste
			if (!Directory.Exists(receivingFolderPath))
			{
				Directory.CreateDirectory(receivingFolderPath);
			}
		}

		/// <summary>
		/// The aim of this method is to convert a unix timestamp, based on
		/// seconds from the epoch, into a DateTime, based on .NET ticks (1 tick = 1 ns)
		/// </summary>
		public static DateTime TimeFromUnixTimestamp(int unixTimestamp)
        {
            DateTime unixYear0 = new DateTime(1970, 1, 1);
            long unixTimeStampInTicks = unixTimestamp * TimeSpan.TicksPerSecond;
            DateTime dtUnix = new DateTime(unixYear0.Ticks + unixTimeStampInTicks);
            return dtUnix;
        }
    }
}
