using Npgsql;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class DatabaseInterface
    {
        NpgsqlConnection conn; 
        private enum SqlEvent {Insert, Update, Delete, Select};
        private enum SqlType { Numeric, ByteArray, String, TimeStamp, Column};
        private const String TMFORMAT = "yyyy-MM-dd HH:mm:ss";

        private class SqlVariable
        {
            
            internal String colname;
            internal String value;
            internal bool where;
            internal SqlVariable (String Colname, String Value, SqlType Type, bool Where)
            {
                colname = Colname;
                value = Value;
                where = Where;
                if (Type == SqlType.String || Type == SqlType.ByteArray || Type == SqlType.TimeStamp)
                    value = "'" + value + "'";
                // if right type insert quotes
            }
        }
        private string getSql(SqlEvent eventtype, String tablename,params SqlVariable[] items)
        {
            String query="";
            switch(eventtype)
            {
                case SqlEvent.Select:
                    {
                        query = "select ";
                        int i = 0;
                        for (; i < items.Length && !items[i].where; i++)
                        {
                            if (i > 0)
                                query += ",";
                            query += (items[i].colname);
                        }
                        query += (" from " + tablename);
                        int start = i;
                        for(; i<items.Length;i++)
                        {
                            if (!items[i].where)
                                throw new Exception("Out of order condition");
                            if (i == start)
                                query += " where ";
                            else
                                query += " and ";
                            query += (items[i].colname + "=" + items[i].value);
                        }
                        break;
                    }
                case SqlEvent.Delete:
                    {
                        query = "delete from " + tablename;
                        for (int i = 0; i < items.Length; i++)
                        {
                            if (i == 0)
                                query += " where ";
                            else
                                query += " and ";
                            query += (items[i].colname + "=" + items[i].value);
                        }
                        break;
                    }
                case SqlEvent.Update:
                   {
                        query = "update " + tablename;
                        int i = 0;
                        for (; i < items.Length && !items[i].where; i++)
                        {
                            if (i == 0)
                                query += " set ";
                            else
                                query += ",";
                            query += (items[i].colname + "=" + items[i].value);
                        }
                        int start = i;
                        for (; i < items.Length; i++)
                        {
                            if (!items[i].where)
                                throw new Exception("Out of order condition");
                            if (i == start)
                                query += " where ";
                            else
                                query += "and ";
                            query += (items[i].colname + "=" + items[i].value);
                        }
                        break;
                    }
                case SqlEvent.Insert:
                    {
                        query = "insert into " + tablename;
                        for (int i = 0; i < items.Length; i++)
                        {
                            if (i == 0)
                                query += "(";
                            else
                                query += ",";
                            query += (items[i].colname);
                        }
                        for (int i = 0; i < items.Length; i++)
                        {
                            if (i == 0)
                                query += ") values (";
                            else
                                query += ",";
                            query += (items[i].value);
                        }
                        query += ")";
                        break;
                    }
            }
            System.Diagnostics.Debug.Print(query);
            return query;
        }
        internal struct StationInfo
        {
            internal String NameMAC;
            internal String RoomName;
            internal Double X;
            internal Double Y;
            internal Byte[] shortInterpolator;
            internal Byte[] longInterpolator;
        }
        internal struct RoomInfo
        {
            internal String RoomName;
            internal double Xlen;
            internal double Ylen;
        }
        public DatabaseInterface(String connectionstring) 
        {
            System.Diagnostics.Debug.Print(connectionstring);
            try
            {
                conn = new NpgsqlConnection(connectionstring);
                conn.Open();
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
            
        }
        internal void close()
        {
            conn.Close();
        }
        
        private bool performNonQuery(String sql)
        {
            int res = 0;
            using (var cmd = new NpgsqlCommand(sql))
            {
                cmd.Connection = conn;
                try
                {
                    res = cmd.ExecuteNonQuery();
                }
                catch (Npgsql.PostgresException ex)
                {
                    if (ex.SqlState == "23505") //Sql state for unique constraint violation
                        res = -1;
                    else
                        throw new Exception(ex.Message);
                }

            }
            return res >= 0;
        }

        internal Nullable<StationInfo> loadStationInfo(String NameMAC)
        {
            Nullable<StationInfo> si;
            String query = getSql(SqlEvent.Select, "stations",
                new SqlVariable("roomname", null, SqlType.Column, false),
                new SqlVariable("xpos", null, SqlType.Column, false),
                new SqlVariable("ypos", null, SqlType.Column, false),
                new SqlVariable("shortintrp", null, SqlType.Column, false),
                new SqlVariable("longintrp", null, SqlType.Column, false),
                new SqlVariable("namemac", NameMAC, SqlType.String, true));
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        StationInfo si2 = new StationInfo();
                        si2.NameMAC = NameMAC;
                        si2.RoomName = reader.GetString(0);
                        si2.X = reader.GetFloat(1);
                        si2.Y = reader.GetFloat(2);
                        si2.shortInterpolator = Convert.FromBase64String(reader.GetString(3));
                        si2.longInterpolator = Convert.FromBase64String(reader.GetString(4));
                        si = si2;
                    }
                    else
                        si = null;
                }
            }
            return si;
        }

        internal IEnumerable<RoomInfo> loadRooms()
        {
            LinkedList<RoomInfo> li = new LinkedList<RoomInfo>();
            String query = getSql(SqlEvent.Select, "rooms",
                new SqlVariable("roomname", null, SqlType.Column, false),
                new SqlVariable("xlength", null, SqlType.Column, false),
                new SqlVariable("ylength", null, SqlType.Column, false));
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        RoomInfo ri = new RoomInfo();
                        ri.RoomName = reader.GetString(0);
                        ri.Xlen = reader.GetFloat(1);
                        ri.Ylen = reader.GetFloat(2);
                        li.AddLast(ri);
                    }
                }
            }
            return li;
        }

        internal bool saveStationInfo(StationInfo si)
        {
            string shint = Convert.ToBase64String(si.shortInterpolator);
            string lgint = Convert.ToBase64String(si.longInterpolator);
            String query = getSql(SqlEvent.Insert, "stations",
                            new SqlVariable("namemac", si.NameMAC, SqlType.String, false),
                            new SqlVariable("roomname", si.RoomName, SqlType.String, false),
                            new SqlVariable("xpos", si.X.ToString(CultureInfo.InvariantCulture), SqlType.Numeric, false),
                            new SqlVariable("ypos", si.Y.ToString(CultureInfo.InvariantCulture), SqlType.Numeric, false),
                            new SqlVariable("shortintrp", shint, SqlType.String, false),
                            new SqlVariable("longintrp", lgint, SqlType.String, false));
            return performNonQuery(query);
        }        

        internal bool saveRoom(String RoomName, double Xlen, double Ylen)
        {
            String query = getSql(SqlEvent.Insert, "rooms",
                            new SqlVariable("roomname", RoomName, SqlType.String, false),
                            new SqlVariable("xlength", Xlen.ToString(CultureInfo.InvariantCulture), SqlType.Numeric, false),
                            new SqlVariable("ylength", Ylen.ToString(CultureInfo.InvariantCulture), SqlType.Numeric, false));
            return performNonQuery(query);
        }

        internal bool deleteRoom(string roomName)
        {
            String query = getSql(SqlEvent.Delete, "rooms", new SqlVariable("roomname", roomName, SqlType.String, true));
            return performNonQuery(query);
        }

        internal bool removeStation(string nameMAC)
        {
            String query = getSql(SqlEvent.Delete, "stations", new SqlVariable("namemac", nameMAC, SqlType.String, true));
            return performNonQuery(query);
        }

        internal bool updateRoomCount(double count, String roomName)
        {
            String query = getSql(SqlEvent.Update, "rooms", 
                new SqlVariable("pcount", count.ToString(CultureInfo.InvariantCulture),  SqlType.Numeric, false), 
                new SqlVariable("roomname", roomName, SqlType.String, true));
            return performNonQuery(query);
        }

        internal bool addLTRoomCount(double stat, String roomname, DateTime timestamp)
        {
            String query = getSql(SqlEvent.Insert, "countstats", 
                new SqlVariable("count", stat.ToString(CultureInfo.InvariantCulture),SqlType.Numeric,false), 
                new SqlVariable("roomname", roomname,  SqlType.String,false), 
                new SqlVariable("tm", timestamp.ToString(TMFORMAT), SqlType.TimeStamp,false));
            return performNonQuery(query);
        }

        internal bool addRequestedSSID(string identifier, string SSID)
        {
            String query = getSql(SqlEvent.Insert, "requestedssids", 
                new SqlVariable("identifier", identifier, SqlType.String, false), 
                new SqlVariable("ssid", SSID, SqlType.String, false));
            return performNonQuery(query);
        }

        internal bool renameDevice(string oldid, string newid)
        {
            String query = getSql(SqlEvent.Update, "requestedssids", 
                new SqlVariable("identifier", newid, SqlType.String, false), 
                new SqlVariable("identifier", oldid, SqlType.String, true));
            bool res = performNonQuery(query);
            query = getSql(SqlEvent.Update, "devicespositions", 
                new SqlVariable("identifier", newid, SqlType.String, false), 
                new SqlVariable("identifier", oldid, SqlType.String, true));
            return res && performNonQuery(query);
        }
        internal bool addDevicePosition(string identifier, string mac, string roomname, double xpos, double ypos, DateTime timestamp)
        {
            String query = getSql(SqlEvent.Insert, "devicespositions", 
                new SqlVariable("identifier", identifier, SqlType.String, false), 
                new SqlVariable("mac", mac, SqlType.String, false),
                new SqlVariable("roomname", roomname, SqlType.String, false),
                new SqlVariable("tm", timestamp.ToString(TMFORMAT), SqlType.TimeStamp, false),
                new SqlVariable("xpos", xpos.ToString(CultureInfo.InvariantCulture), SqlType.Numeric, false),
                new SqlVariable("ypos", ypos.ToString(CultureInfo.InvariantCulture), SqlType.Numeric, false));
            return performNonQuery(query);
        }
    }
}
