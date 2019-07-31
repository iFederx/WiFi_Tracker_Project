using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panopticon
{
    class FileParser
    {
        /// <summary>
        /// This static method reads an ESP input file and returns a List of Packet objects
        /// </summary>
        public static Dictionary<String, Packet> Parse(String filePath, Station receivingStation)
        {
            string line;
            StreamReader file = new StreamReader(@filePath);
            Dictionary<String,Packet> packets = new Dictionary<string, Packet>();
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
                            SRC = fieldSplit[1];
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
                
                if (HASH != "" && HASH.Length < 40)
                { 
                    if (packets.ContainsKey(HASH))
                    {
                        //if a packet is present, I add a Reception to it
                        packets[HASH].received(receivingStation, double.Parse(RSSI)); //si da solo crea internamente un oggetto Reception
                    }
                    else
                    {
                        //else, I instantiate a new Packet
                        Packet tempPacket = new Packet(SRC, SSID, TimeFromUnixTimestamp(int.Parse(TIME)), HASH, HT_cap_str, long.Parse(seq_num));
                        tempPacket.received(receivingStation, double.Parse(RSSI));
                        packets.Add(HASH, tempPacket);
                        
                    }
                }
            }
			file.Close();
            return packets;
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
