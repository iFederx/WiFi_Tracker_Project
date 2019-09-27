﻿using System;
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
		private Dictionary<string, FileSystemWatcher> watchers;
		ConcurrentDictionary<string, MetaPacket> metaPackets; //the key is the hash
		volatile bool killed = false;
		int sleepTime = 30000;
		volatile int stationGC = 0;
		ConcurrentDictionaryStack<string, Packet> cds;
		Func<Packet, Packet, Packet> packetReducer =
			(a, b) =>
			{
				//a e b sono due Packet che, se hanno lo stesso HASH, devono essere fusi insieme
				//a è il valore che era già presente nella coda
				bool isNewStation = true;

				foreach (Packet.Reception rec in a.Receivings)
				{
					//controllo se nel Packet c'è già una Reception dalla station di B
					if (rec.ReceivingStation.Equals(b.Receivings[0].ReceivingStation))
					{
						isNewStation = false;
						break;
					}
				}
				if (isNewStation)
					a.received(b.Receivings[0].ReceivingStation, b.Receivings[0].RSSI);
				
				return a;
			};

		Func<Packet, bool> isReady =
			(pak) =>
			{
				//ritorno true se il packet è stato ricevuto da tutte le stazioni nella stanza
				Room r = pak.Receivings[0].ReceivingStation.location.room;
				if (pak.Receivings.Count == r.stationcount)
					return true;
				else
					return false;
			};

		public FileParser(Context _ctx)
		{
			ctx = _ctx;
			watchers = new Dictionary<string,FileSystemWatcher>();
			metaPackets = new ConcurrentDictionary<string, MetaPacket>();
			cds = new ConcurrentDictionaryStack<string, Packet>();
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
			foreach (string key in watchers.Keys)
			{
				watchers[key].Dispose();
				watchers.Remove(key);
			}

			killed = true;
		}

		internal void AddWatcher(string _mac) //metodo da chiamare per ogni Station creata
		{
			if (!watchers.ContainsKey(_mac))
			{
				var watcher = new FileSystemWatcher();
				watcher.Path = "./Received/" + _mac + "/";
				watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
				watcher.Filter = "*.txt";
				watcher.Changed += new FileSystemEventHandler(OnChanged);
				watcher.EnableRaisingEvents = true;
				watchers.Add(_mac, watcher);
			}
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
				s.hearbeat();
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
			ctx.checkStationAliveness(receivingStation.location.room);
			while ((line = file.ReadLine()) != null)
            {
                string Type = "", SubType = "", RSSI = "", SRC = "", seq_num = "", TIME = "", HASH = "", SSID_id = "", SSID_lenght = "", SSID = "", HT_id = "", HT_cap_len = "", HT_cap_str = "";
                var fields = line.Split(',');
                string[] pures = fields;
                foreach (string field in fields) //each cycle reads a line
                {
                    var fieldSplit = field.Split('=');
					
                    string fieldLabel = fieldSplit[0];
                    switch (fieldLabel)
                    {
                        case "Type":
							pures = fieldSplit[1].Split('\0');
							Type = pures[0];
                            break;

                        case " SubType":
							pures = fieldSplit[1].Split('\0');
							SubType = pures[0];
                            break;

                        case " RSSI":
							pures = fieldSplit[1].Split('\0');
							RSSI = pures[0];
                            break;

                        case " SRC":
							pures = fieldSplit[1].Split('\0');
							SRC = pures[0].Replace(@":", string.Empty);
                            break;

                        case " seq_num":
							pures = fieldSplit[1].Split('\0');
							seq_num = pures[0];
                            break;

                        case " TIME":
							pures = fieldSplit[1].Split('\0');
							TIME = pures[0];
                            break;

                        case " HASH":
							pures = fieldSplit[1].Split('\0');
							HASH = pures[0];
                            break;

                        case " SSID_id":

							pures = fieldSplit[1].Split('\0');
							SSID_id = pures[0];
                            break;

                        case " SSID_lenght":
							pures = fieldSplit[1].Split('\0');
							SSID_lenght = pures[0];
                            break;

                        case " SSID":
							pures = fieldSplit[1].Split('\0');
							SSID = pures[0];
                            break;

                        case " HT_id":
							pures = fieldSplit[1].Split('\0');
							HT_id = pures[0];
                            break;

                        case " HT_cap_len":
							pures = fieldSplit[1].Split('\0');
							HT_cap_len = pures[0];
                            break;

                        case " HT_cap_str":
							pures = fieldSplit[1].Split('\0');
							HT_cap_str = pures[0];
                            break;
                    }
                }
				
                if (HASH!="" && HASH.Length<40 && RSSI!="" && TIME!="" && TIME.Length==10 && seq_num!="")
                {
					/* 1. creare un pacchetto nuovo e completo ogni volta
					 * 2. chiami upsert di ConcurrentDictionaryStack.
					 *		2a. Come funzione updater, in upsert metti una funzione che prende uno
					 *			dei due pacchetti e ci aggiunge (in stile addAll) la lista delle receinving
					 *			dell'altro pacchetto, e restituisce quello con più receiving.
					 *		2b. Dopodichè tu riceverai da upsert il pacchetto effettivamente aggiunto, e potrai
					 *			quindi valutare se è stato ricevuto da tutti o solo da qualcuno.
					 */

					Packet packet = new Packet(SRC, SSID, TimeFromUnixTimestamp(int.Parse(TIME)), HASH, HT_cap_str, long.Parse(seq_num));
					packet.received(receivingStation, double.Parse(RSSI));
					if (cds.upsertAndConditionallyRemove(HASH, packet, packetReducer, isReady, out packet))
						ctx.getAnalyzer().sendToAnalysisQueue(packet);


					/*if (!packets.ContainsKey(HASH))
					{
						packet.received(receivingStation, double.Parse(RSSI));
						packets.Add(HASH, packet);
					}*/
                }
            }
			file.Close();

			

				//aggiungo i dati del file appena letto, alla struttura thread-safe
				/*foreach (Packet pak in packets.Values)
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
				}	*/
		}

		public void ParseOnTheFly(String input, Station receivingStation)
		{
			Dictionary<String, Packet> packets = new Dictionary<string, Packet>(); //the key is the hash
			int discarded = 0;
			//ctx.checkStationAliveness(receivingStation.location.room); //TODO: forse meglio chiamarlo prima di chiamare questa funzione
			foreach (string line in input.Split('\n'))
			{
				string Type = "", SubType = "", RSSI = "", SRC = "", seq_num = "", TIME = "", HASH = "", SSID_id = "", SSID_lenght = "", SSID = "", HT_id = "", HT_cap_len = "", HT_cap_str = "";
				var fields = line.Split(',');
				string[] pures = fields;
				foreach (string field in fields) //each cycle reads a line
				{
					var fieldSplit = field.Split('=');

					string fieldLabel = fieldSplit[0];
					switch (fieldLabel)
					{
						case "Type":
							pures = fieldSplit[1].Split('\0');
							Type = pures[0];
							break;

						case " SubType":
							pures = fieldSplit[1].Split('\0');
							SubType = pures[0];
							break;

						case " RSSI":
							pures = fieldSplit[1].Split('\0');
							RSSI = pures[0];
							break;

						case " SRC":
							pures = fieldSplit[1].Split('\0');
							SRC = pures[0].Replace(@":", string.Empty);
							break;

						case " seq_num":
							pures = fieldSplit[1].Split('\0');
							seq_num = pures[0];
							break;

						case " TIME":
							pures = fieldSplit[1].Split('\0');
							TIME = pures[0];
							break;

						case " HASH":
							pures = fieldSplit[1].Split('\0');
							HASH = pures[0];
							break;

						case " SSID_id":

							pures = fieldSplit[1].Split('\0');
							SSID_id = pures[0];
							break;

						case " SSID_lenght":
							pures = fieldSplit[1].Split('\0');
							SSID_lenght = pures[0];
							break;

						case " SSID":
							pures = fieldSplit[1].Split('\0');
							SSID = pures[0];
							break;

						case " HT_id":
							pures = fieldSplit[1].Split('\0');
							HT_id = pures[0];
							break;

						case " HT_cap_len":
							pures = fieldSplit[1].Split('\0');
							HT_cap_len = pures[0];
							break;

						case " HT_cap_str":
							pures = fieldSplit[1].Split('\0');
							HT_cap_str = pures[0];
							break;
					}
				}

				int n;
				if (HASH != "" && HASH.Length == 32 && RSSI != "" && TIME.Length == 10 && seq_num != "" && SRC.Length == 12 && SSID != "" && HT_cap_str != "" && int.TryParse(RSSI, out n)) //pre-processing
				{
					Packet packet = new Packet(SRC, SSID, TimeFromUnixTimestamp(int.Parse(TIME)), HASH, HT_cap_str, long.Parse(seq_num));
					packet.received(receivingStation, double.Parse(RSSI));
					if (cds.upsertAndConditionallyRemove(HASH, packet, packetReducer, isReady, out packet))
						ctx.getAnalyzer().sendToAnalysisQueue(packet);
				}
				else discarded++;
			}
			if (discarded > 0) System.Console.WriteLine("Scartati: {0} pacchetti", discarded);
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
