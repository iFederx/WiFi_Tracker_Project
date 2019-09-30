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
			cds = new ConcurrentDictionaryStack<string, Packet>();
		}

		public void ParseOnTheFly(String input, Station receivingStation)
		{
			Dictionary<String, Packet> packets = new Dictionary<string, Packet>(); //the key is the hash
			int discarded = 0;
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
