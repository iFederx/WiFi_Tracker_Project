using Npgsql;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panopticon
{
    class DatabaseInterface
    {
        NpgsqlConnection conn; 
        private enum SqlEvent {Insert, Update, Delete, Select};
        private enum SqlType { Numeric, ByteArray, String, TimeStamp, Column,Boolean};
        private const String TMFORMAT = "yyyy-MM-dd HH:mm:ss";
        private Object dblock = new object();
        private class SqlVariable
        {
            
            internal String colname;
            internal String value;
            internal bool where;
            internal string whereclause;
            internal SqlVariable(String Colname) : this(Colname, null, SqlType.Column, false, "=")
            {
            }
            internal SqlVariable (String Colname, String Value, SqlType Type):this(Colname,Value,Type,false,"=")
            {
            }
            internal SqlVariable(String Colname, String Value, SqlType Type, Boolean Where):this(Colname,Value,Type,Where,"=")
            {
            }
            internal SqlVariable(String Colname, String Value, SqlType Type, Boolean Where, String WhereClause)
            {
                colname = Colname;
                value = Value;
                if (Type == SqlType.String || Type == SqlType.ByteArray || Type == SqlType.TimeStamp)
                    value = "'" + value + "'";
                where = Where;
                whereclause = WhereClause;
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
                            query += (items[i].colname + items[i].whereclause + items[i].value);
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
            lock (dblock)
            {
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
            }
            return res >= 0;
        }

        internal Nullable<StationInfo> loadStationInfo(String NameMAC)
        {
            Nullable<StationInfo> si;
            String query = getSql(SqlEvent.Select, "stations",
                new SqlVariable("roomname", null, SqlType.Column),
                new SqlVariable("xpos", null, SqlType.Column),
                new SqlVariable("ypos", null, SqlType.Column),
                new SqlVariable("shortintrp", null, SqlType.Column),
                new SqlVariable("longintrp", null, SqlType.Column),
                new SqlVariable("namemac", NameMAC, SqlType.String, true));
            lock (dblock)
            {
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
            }
            return si;
        }

        internal IEnumerable<RoomInfo> loadRooms()
        {
            LinkedList<RoomInfo> li = new LinkedList<RoomInfo>();
            String query = getSql(SqlEvent.Select, "rooms",
                new SqlVariable("roomname", null, SqlType.Column),
                new SqlVariable("xlength", null, SqlType.Column),
                new SqlVariable("ylength", null, SqlType.Column));
            lock (dblock)
            {
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
            }
            return li;
        }

        internal bool saveStationInfo(StationInfo si)
        {
            string shint = Convert.ToBase64String(si.shortInterpolator);
            string lgint = Convert.ToBase64String(si.longInterpolator);
            String query = getSql(SqlEvent.Insert, "stations",
                            new SqlVariable("namemac", si.NameMAC, SqlType.String),
                            new SqlVariable("roomname", si.RoomName, SqlType.String),
                            new SqlVariable("xpos", si.X.ToString(CultureInfo.InvariantCulture), SqlType.Numeric),
                            new SqlVariable("ypos", si.Y.ToString(CultureInfo.InvariantCulture), SqlType.Numeric),
                            new SqlVariable("shortintrp", shint, SqlType.String),
                            new SqlVariable("longintrp", lgint, SqlType.String));
            return performNonQuery(query);
        }        

        internal bool saveRoom(String RoomName, double Xlen, double Ylen)
        {
            String query = getSql(SqlEvent.Insert, "rooms",
                            new SqlVariable("roomname", RoomName, SqlType.String),
                            new SqlVariable("xlength", Xlen.ToString(CultureInfo.InvariantCulture), SqlType.Numeric),
                            new SqlVariable("ylength", Ylen.ToString(CultureInfo.InvariantCulture), SqlType.Numeric));
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
                new SqlVariable("pcount", count.ToString(CultureInfo.InvariantCulture),  SqlType.Numeric), 
                new SqlVariable("roomname", roomName, SqlType.String, true));
            return performNonQuery(query);
        }

        internal bool addLTRoomCount(double stat, String roomname, DateTime timestamp)
        {
            String query = getSql(SqlEvent.Insert, "countstats", 
                new SqlVariable("count", stat.ToString(CultureInfo.InvariantCulture),SqlType.Numeric), 
                new SqlVariable("roomname", roomname,  SqlType.String), 
                new SqlVariable("tm", timestamp.ToString(TMFORMAT), SqlType.TimeStamp));
            return performNonQuery(query);
        }

        internal bool addRequestedSSID(string identifier, string SSID)
        {
            String query = getSql(SqlEvent.Insert, "requestedssids", 
                new SqlVariable("identifier", identifier, SqlType.String), 
                new SqlVariable("ssid", SSID, SqlType.String));
            return performNonQuery(query);
        }

        internal bool renameDevice(string oldid, string newid)
        {
            String query = getSql(SqlEvent.Update, "requestedssids", 
                new SqlVariable("identifier", newid, SqlType.String), 
                new SqlVariable("identifier", oldid, SqlType.String, true));
            bool res = performNonQuery(query);
            query = getSql(SqlEvent.Update, "devicespositions", 
                new SqlVariable("identifier", newid, SqlType.String), 
                new SqlVariable("identifier", oldid, SqlType.String, true));
            return res && performNonQuery(query);
        }
        internal bool addDevicePosition(string identifier, string mac, string roomname, double xpos, double ypos, DateTime timestamp, Publisher.EventType evty)
        {
            String query = getSql(SqlEvent.Insert, "devicespositions", 
                new SqlVariable("identifier", identifier, SqlType.String), 
                new SqlVariable("mac", mac, SqlType.String),
                new SqlVariable("roomname", roomname, SqlType.String),
                new SqlVariable("tm", timestamp.ToString(TMFORMAT), SqlType.TimeStamp),
                new SqlVariable("xpos", xpos.ToString(CultureInfo.InvariantCulture), SqlType.Numeric),
                new SqlVariable("ypos", ypos.ToString(CultureInfo.InvariantCulture), SqlType.Numeric),
                new SqlVariable("outmovement",(evty==Publisher.EventType.Disappear||evty==Publisher.EventType.MoveOut).ToString(),SqlType.Boolean));
            return performNonQuery(query);
        }
        internal struct DevicePosition
        {
            internal String identifier;
            internal double xpos;
            internal double ypos;
            internal double prexpos;
            internal double preypos;
            internal DateTime timestamp;
            internal bool moveout;
        }
        internal DevicePosition[] loadDevicesPositions(string roomName, DateTime fromdate, string fromtime, DateTime todate, string totime)
        {
            String query = getSql(SqlEvent.Select, "devicespositions",
                new SqlVariable("identifier", null,SqlType.Column),
                new SqlVariable("xpos", null, SqlType.Column),
                new SqlVariable("ypos", null, SqlType.Column),
                new SqlVariable("tm", null, SqlType.Column),
                new SqlVariable("outmovement", null, SqlType.Column),
                new SqlVariable("roomname", roomName, SqlType.String,true),
                new SqlVariable("tm", fromdate.ToString("yyyy-MM-dd") + " " + fromtime, SqlType.TimeStamp, true, ">="),
                new SqlVariable("tm", todate.ToString("yyyy-MM-dd") + " " + totime, SqlType.TimeStamp, true, "<=")) +
                " order by tm asc";
            LinkedList<DevicePosition> li = new LinkedList<DevicePosition>();
            Dictionary<String, DevicePosition> prepos = new Dictionary<String, DevicePosition>();
            lock (dblock)
            {
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DevicePosition dp = new DevicePosition();
                            DevicePosition dp2;
                            dp.identifier = reader.GetString(0);
                            dp.xpos = reader.GetDouble(1);
                            dp.ypos = reader.GetDouble(2);
                            dp.timestamp = reader.GetDateTime(3);
                            dp.moveout = reader.GetBoolean(4);
                            if (prepos.TryGetValue(dp.identifier, out dp2))
                            {
                                dp.prexpos = dp2.xpos;
                                dp.preypos = dp2.ypos;
                                prepos[dp.identifier] = dp;
                            }
                            else
                            {
                                dp.prexpos = -1;
                                dp.preypos = -1;
                                prepos.Add(dp.identifier, dp);
                            }
                            li.AddLast(dp);
                        }
                    }
                }
            }
            return li.ToArray<DevicePosition>();


        }

        internal double[] loadMaxDevicesDay(int selectedmonth, int selectedyear, String roomname)
        {
            int numdays = 31;
            if (selectedmonth == 2)
            {
                numdays = 28;
                if (selectedyear % 4 == 0 && (selectedyear % 100 != 0 || selectedyear % 1000 == 0))
                    numdays = 29;
            }
            else if (selectedmonth == 4 || selectedmonth == 6 || selectedmonth == 9 || selectedmonth == 11)
                numdays = 30;
            double[] res = new double[numdays+1];
            String query = getSql(SqlEvent.Select, "countstats",
                new SqlVariable("max(count) as mcount"),
                new SqlVariable("extract(day from tm) as dday"),
                new SqlVariable("roomname", roomname, SqlType.String, true),
                new SqlVariable("extract(month from tm)", selectedmonth.ToString(), SqlType.Numeric, true),
                new SqlVariable("extract(year from tm)", selectedyear.ToString(), SqlType.Numeric, true)) + " group by extract(day from tm)";
            bool notempty = false;
            lock (dblock)
            {
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            notempty = true;
                            res[(int)reader.GetDouble(1)] = reader.GetDouble(0);
                        }
                    }
                }
            }
            return notempty?res:null;
        }

        internal double[][] loadAvgDevicesTime(int selectedmonth, int selectedyear, string roomname)
        {
            int numdays = 31;
            if (selectedmonth == 2)
            {
                numdays = 28;
                if (selectedyear % 4 == 0 && (selectedyear % 100 != 0 || selectedyear % 1000 == 0))
                    numdays = 29;
            }
            else if (selectedmonth == 4 || selectedmonth == 6 || selectedmonth == 9 || selectedmonth == 11)
                numdays = 30;
            double[][] res = new double[numdays + 1][];
            for(int i=0;i<numdays+1;i++)
                res[i]=new double[24];
            String query = getSql(SqlEvent.Select, "countstats",
                new SqlVariable("avg(count) as mcount"),
                new SqlVariable("extract(day from tm) as dday"),
                new SqlVariable("extract(hour from tm) as dhour"),
                new SqlVariable("roomname", roomname, SqlType.String, true),
                new SqlVariable("extract(month from tm)", selectedmonth.ToString(), SqlType.Numeric, true),
                new SqlVariable("extract(year from tm)", selectedyear.ToString(), SqlType.Numeric, true)) + " group by extract(hour from tm), extract(day from tm)";

            lock (dblock)
            {
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            res[(int)reader.GetDouble(1)][(int)reader.GetDouble(2)] = reader.GetDouble(0);
                        }
                    }
                }
            }
            for(int hour=0;hour<24;hour++)
            {
                for (int day = 1; day < res.GetLength(0);day++)
                    res[0][hour] += res[day][hour];
                res[0][hour] /= res.GetLength(0);
            }
            return res;
        }

        internal int[][,] loadHeathmaps(object p, string roomname, double xlength, double ylength, int selectedmonth, int selectedyear)
        {
            int numdays = 31;
            if (selectedmonth == 2)
            {
                numdays = 28;
                if (selectedyear % 4 == 0 && (selectedyear % 100 != 0 || selectedyear % 1000 == 0))
                    numdays = 29;
            }
            else if (selectedmonth == 4 || selectedmonth == 6 || selectedmonth == 9 || selectedmonth == 11)
                numdays = 30;
            int[][,] res = new int[numdays + 1][,];
            for(int j=0;j<numdays+1;j++)
                res[j]= new int[(int)xlength+1, (int)ylength + 1];
            String query = getSql(SqlEvent.Select, "devicespositions",
               new SqlVariable("xpos"),
               new SqlVariable("ypos"),
               new SqlVariable("extract(day from tm) as dday"),
               new SqlVariable("roomname", roomname, SqlType.String, true),
               new SqlVariable("extract(month from tm)", selectedmonth.ToString(), SqlType.Numeric, true),
               new SqlVariable("extract(year from tm)", selectedyear.ToString(), SqlType.Numeric, true));
            lock (dblock)
            {
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            res[(int)(reader.GetDouble(2))][(int)(reader.GetDouble(0)), (int)(reader.GetDouble(1))] += 1;
                        }
                    }
                }
            }
            for (int xpos = 0; xpos < res[0].GetLength(0); xpos++)
            {
                for(int ypos=0;ypos<res[0].GetLength(1);ypos++)
                {
                    for (int day = 1; day < res.Length; day++)
                        res[0][xpos,ypos] += res[day][xpos,ypos];
                }               
            }
            return res;
        }

        internal string[,] loadFrequentMacs(int month, int year, string roomname)
        {
            int maxres = 15;
            int numdays = 31;
            if (month == 2)
            {
                numdays = 28;
                if (year % 4 == 0 && (year % 100 != 0 || year % 1000 == 0))
                    numdays = 29;
            }
            else if (month == 4 || month == 6 || month == 9 || month == 11)
                numdays = 30;
            String[,] res = new String[numdays + 1, maxres];
            String query1 = getSql(SqlEvent.Select, "devicespositions",
               new SqlVariable("identifier"),
               new SqlVariable("count(*) as cnt"),
               new SqlVariable("extract(day from date(tm)) as dt"),
               new SqlVariable("roomname", roomname, SqlType.String, true),
               new SqlVariable("extract(month from tm)", month.ToString(), SqlType.Numeric, true),
               new SqlVariable("extract(year from tm)", year.ToString(), SqlType.Numeric, true))+" group by identifier,date(tm)";
            String query2 = getSql(SqlEvent.Select, "devicespositions",
               new SqlVariable("identifier"),
               new SqlVariable("count(*) as cnt"),
               new SqlVariable("0 as dt"),
               new SqlVariable("roomname", roomname, SqlType.String, true),
               new SqlVariable("extract(month from tm)", month.ToString(), SqlType.Numeric, true),
               new SqlVariable("extract(year from tm)", year.ToString(), SqlType.Numeric, true)) + " group by identifier";
            String query = "(" + query1 + ") union (" + query2 + ") order by dt,cnt desc";
            int preday = -1;
            int cntperday = 0;
            lock (dblock)
            {
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int day = (int)reader.GetDouble(2);
                            String id = reader.GetString(0);
                            if (preday != day)
                            {
                                cntperday = 0;
                                preday = day;
                            }
                            if(cntperday<maxres)
                            {
                                res[day, cntperday] = id;
                                cntperday++;
                            }
                        }
                    }
                }
            }
            return res;
        }

        internal class DeviceInfo
        {
            internal DateTime FirstSeen;
            internal DateTime LastSeen;
            internal String[] ssids;
        }

        internal DeviceInfo loadDeviceInfo(string id)
        {
            DeviceInfo di = new DeviceInfo();
            String query1 = getSql(SqlEvent.Select, "devicespositions",
                new SqlVariable("min(tm)"),
                new SqlVariable("max(tm)"),
                new SqlVariable("identifier", id, SqlType.String, true));
            String query2 = getSql(SqlEvent.Select, "requestedssids",
                new SqlVariable("ssid"),
                new SqlVariable("identifier", id, SqlType.String, true));
            LinkedList<String> ssids = new LinkedList<string>();
            lock (dblock)
            {
                using (var cmd = new NpgsqlCommand(query1, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            try
                            {
                                di.FirstSeen = reader.GetDateTime(0);
                                di.LastSeen = reader.GetDateTime(1);
                            }
                            catch(Exception ex)
                            {
                                return null;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                using (var cmd = new NpgsqlCommand(query2, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ssids.AddLast(reader.GetString(0));
                        }
                    }
                }
            }
            di.ssids = ssids.ToArray<String>();
            return di;
        }

        internal class DeviceStats
        {
            internal Double[] timeperday;
            internal Double[] pingsperhour;
            internal int[,] heatmap;
            internal Dictionary<String, Int32> roommap = new Dictionary<string, int>();
        }

        internal DeviceStats loadDeviceStats(DateTime fromdate, string fromtime, DateTime todate, string totime, string deviceid, string roomName, bool loadroommap, bool loadheathmap, double xlen, double ylen)
        {
            DeviceStats ds = new DeviceStats();
            return ds;
        }
    }
}
