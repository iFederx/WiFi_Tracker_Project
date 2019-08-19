using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Panopticon.DatabaseInterface;
using static Panopticon.Publisher;

namespace Panopticon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    partial class MainWindow : Window
    {
        Context ctx;
        Object guilock = new Object();
        RoomInfoGUI selectedRoom = null;
        Dictionary<Room, RoomInfoGUI> roomToRoomInfoGUI = new Dictionary<Room, RoomInfoGUI>();
        Dictionary<object, List<UIElement>> uiElements = new Dictionary<object, List<UIElement>>();
        Stats loadedstats = null;
        Replay loadedreplay = null;
        String[] months = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
        Brush[] weekhistcolors = { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Brown, Brushes.Orange, Brushes.Violet, Brushes.Pink};
        Brush[] timehistcolors = { Brushes.Red, Brushes.Green };
        internal class Stats
        {
            internal int selectedday;
            internal int selectedmonth;
            internal int selectedyear;
            internal double[] maxperday;
            internal double[][] avgperhour = null;
            internal int[][,] heatmaps = null;
            internal Room refroom;
            internal String[,] macs = null;
            public Stats(int month, int year, Room room)
            {
                selectedday = 0;
                selectedmonth = month;
                selectedyear = year;
                refroom = room;
            }
        }
        class Replay
        {
            internal DevicePosition[] replaydata;
            internal double intersec = 1;
            internal int indexupto = 0;
            internal DateTime at;
            internal DateTime startingpoint;
            internal bool playing = false;
            internal System.Windows.Threading.DispatcherTimer timer;
            internal Dictionary<object, List<UIElement>> uiElements = new Dictionary<object, List<UIElement>>();
            internal Room refroom;
            public Replay(DevicePosition[] devicePosition, Room room)
            {
                replaydata = devicePosition;
                startingpoint = replaydata[0].timestamp;
                at = startingpoint;
                refroom = room;
            }
        }
        class RoomInfoGUI
        {
            internal Border container;
            internal TextBlock roomname;
            internal TextBlock peoplecount;
            internal TextBlock stationcount;
            internal Room room;
            internal Queue<Double> statsqueue = new Queue<Double>();
            internal Queue<DateTime> statsTimeQueue = new Queue<DateTime>();
        }

        internal void updateDevicePosition(Device d, PositionTools.Position lastPosition, EventType ev)
        {
            drawDevice(d.identifier, lastPosition, ev, lvtrck_canvas,lvtrck_devlist_panel,uiElements,lvtrck_people);
        }
        
        private void drawDevice(String deviceIdentifier, PositionTools.Position lastPosition, EventType ev, Canvas canvas, StackPanel panel, Dictionary<object,List<UIElement>> uielem,Label counter)
        {
            
            lock(guilock)
            {
                List<UIElement> d_gui;
                if((ev == EventType.Appear || ev ==EventType.MoveIn || ev == EventType.Update)&&lastPosition.room==selectedRoom.room)
                {
                    if(!uielem.TryGetValue(deviceIdentifier, out d_gui))
                    {
                        d_gui = new List<UIElement>();
                        Ellipse d_gui_e = new Ellipse();
                        d_gui_e.Height = selectedRoom.room.xlength / 2;
                        d_gui_e.Width = selectedRoom.room.ylength / 2;
                        d_gui_e.ToolTip = "Device " + deviceIdentifier;
                        d_gui_e.Tag = deviceIdentifier;
                        Brush color = Utilities.FancyColorCreator.randomBrush(deviceIdentifier.GetHashCode());
                        d_gui_e.Fill = color.Clone();
                        if (lastPosition.uncertainity == double.MaxValue)
                            d_gui_e.Fill.Opacity = 0;
                        d_gui_e.Stroke = d_gui_e.Fill;
                        d_gui_e.Cursor = Cursors.Hand;
                        d_gui_e.MouseDown += opendeviceinfo;
                        d_gui.Add(d_gui_e);
                        TextBlock lab = new TextBlock();
                        lab.Text = "Device " + deviceIdentifier;
                        lab.FontSize = 16;
                        lab.Foreground = color.Clone();
                        lab.Tag = deviceIdentifier;
                        lab.Cursor = Cursors.Hand;
                        lab.MouseDown += opendeviceinfo;
                        d_gui.Add(lab);
                        panel.Children.Add(lab);
                        canvas.Children.Add(d_gui_e);
                        uielem.Add(deviceIdentifier, d_gui);
                    }
                    Canvas.SetLeft(d_gui[0], lastPosition.X * canvas.Width / selectedRoom.room.xlength);
                    Canvas.SetTop(d_gui[0], lastPosition.Y * canvas.Height / selectedRoom.room.ylength);
                }
                else
                {
                    if (uielem.TryGetValue(deviceIdentifier, out d_gui))
                    {
                        canvas.Children.Remove(d_gui[0]);
                        panel.Children.Remove(d_gui[1]);
                        uielem.Remove(deviceIdentifier);
                    }
                    //else ignore
                }
                if(counter!=null)
                    counter.Content = panel.Children.Count + " devices";
            }
        }

        internal void updateOneSecondDeviceCount(Room r, double stat)
        {
            lock(guilock)
            {
                RoomInfoGUI rgui;
                if (!roomToRoomInfoGUI.TryGetValue(r,out rgui))
                    return;
                rgui.peoplecount.Text = stat + " devices";
            }
        }

        internal void updateTenMinutesDeviceCount(Room r, double stat)
        {
            RoomInfoGUI rg = roomToRoomInfoGUI[r];
            rg.statsqueue.Enqueue(stat);
            rg.statsTimeQueue.Enqueue(DateTime.Now);
            if (rg.statsqueue.Count > 144)
            {
                rg.statsqueue.Dequeue();
                rg.statsTimeQueue.Dequeue();
            }
            if (selectedRoom != null && r == selectedRoom.room)
                drawStats(rg);
        }

        private void drawStats(RoomInfoGUI rg)
        {
            Graphics.drawHistogram(occup_histo, rg.statsqueue, (double d, int i, object o) => { return ((DateTime[])o)[i].ToString("dd/MM HH:mm") + " - " + d.ToString("G2"); }, rg.statsTimeQueue.ToArray(), null, timehistcolors, timehistcolors, 0,null,null);
        }


        private void selectRoom(object sender,MouseButtonEventArgs e)
        {
            lock(guilock) //avoid pre-emption in the middle of redrawing
            {
                if (selectedRoom != null)
                    selectedRoom.container.Background = null;
                loadedstats = null;
                updateReplayControls("No loaded replay.", ReplayState.NotLoaded);
                selectedRoom = (RoomInfoGUI)((Border)sender).Tag;
                ctx.guiPub.linkedroom = selectedRoom.room;
                lvtrck_roomname.Content = selectedRoom.room.roomName;
                trackrlp_roomname.Content = selectedRoom.room.roomName;
                rmstats_roomname.Content = selectedRoom.room.roomName;
                lvtrck_stations.Content = selectedRoom.stationcount.Text;
                lvtrck_people.Content = selectedRoom.peoplecount.Text;
                lvtrck.IsEnabled = selectedRoom.room!=Room.externRoom?true:false;
                trackrlp.IsEnabled = selectedRoom.room != Room.externRoom ? true : false;
                tabControl.SelectedIndex = selectedRoom.room != Room.externRoom ? tabControl.SelectedIndex<3? tabControl.SelectedIndex: 0 : 2;
               
                trackrlp.Visibility = Visibility.Visible;
                rmstats.Visibility = Visibility.Visible;
                selectedRoom.container.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#e5e5e5");
                lvtrck_canvas.Children.Clear();
                lvtrck_devlist_panel.Children.Clear();
                uiElements.Clear();
                resetRmStats(true);

                if (selectedRoom.room != Room.externRoom)
                {
                    lvtrck_viewbox.Child = null;
                    lvtrck_border.Width = selectedRoom.room.xlength * 20.5;
                    lvtrck_border.Height = selectedRoom.room.ylength * 20.5;
                    lvtrck_canvas.Width = selectedRoom.room.xlength * 20;
                    lvtrck_canvas.Height = selectedRoom.room.ylength * 20;
                    lvtrck_viewbox.Child = lvtrck_border;
                    trackrlp_viewbox.Child = null;
                    trackrlp_border.Width = selectedRoom.room.xlength * 20.5;
                    trackrlp_border.Height = selectedRoom.room.ylength * 20.5;
                    trackrlp_canvas.Width = selectedRoom.room.xlength * 20;
                    trackrlp_canvas.Height = selectedRoom.room.ylength * 20;
                    trackrlp_viewbox.Child = trackrlp_border;
                    rmstats_viewbox.Visibility = Visibility.Visible;
                    rmstats_heatlabel.Visibility = Visibility.Visible;
                    rmstats_viewbox.Child = null;
                    rmstats_border.Width = selectedRoom.room.xlength * 20;
                    rmstats_border.Height = selectedRoom.room.ylength * 20;
                    rmstats_viewbox.Child = rmstats_border;
                    foreach (Station s in selectedRoom.room.getStations())
                        drawStation(s);
                    foreach (Device d in selectedRoom.room.getDevices())
                        drawDevice(d.identifier, d.lastPosition, EventType.MoveIn, lvtrck_canvas, lvtrck_devlist_panel, uiElements, lvtrck_people);
                    drawStats(selectedRoom);
                }
                else
                {
                    rmstats_viewbox.Visibility = Visibility.Hidden;
                    rmstats_heatlabel.Visibility = Visibility.Hidden;
                }
                if (tabControl.SelectedIndex == 2)
                    loadRoomStats(DateTime.Now.Month, DateTime.Now.Year, selectedRoom.room);
            }
        }

        

        internal void updateStation(Room r, Station s, Publisher.EventType e)
        {
            lock (guilock)
            {
                RoomInfoGUI rig = roomToRoomInfoGUI[r];
                int stationcount = r.stationcount;
                if (selectedRoom!=null && selectedRoom.room == r)
                {
                    Station[] stations = r.getStations();
                    stationcount = stations.Length;
                    lvtrck_stations.Content = stationcount + " stations";
                    if (e == Publisher.EventType.Appear)
                        drawStation(s);
                    else
                    {
                        UIElement s_gui = uiElements[s][0];
                        if(s_gui!=null)
                        {
                            uiElements.Remove(s);
                            lvtrck_canvas.Children.Remove(s_gui);
                        }
                    }
                }
                rig.stationcount.Text = stationcount + " stations";
            }
        }

        private void drawStation(Station s)
        {
            if (uiElements.ContainsKey(s)) // could be that room just loaded with a new station, and this information arrives later
                return;
            Rectangle s_gui = new Rectangle();
            s_gui.Height = selectedRoom.room.xlength / 3;
            s_gui.Width = selectedRoom.room.ylength / 3;
            s_gui.ToolTip = "Station " + s.NameMAC;
            s_gui.Tag = s;
            s_gui.Fill = Brushes.Red;
            s_gui.Stroke = Brushes.Red;
            s_gui.Cursor = Cursors.Hand;
            lock(guilock)
            {
                Canvas.SetLeft(s_gui, s.location.X * lvtrck_canvas.Width / selectedRoom.room.xlength);
                Canvas.SetTop(s_gui, s.location.Y * lvtrck_canvas.Height / selectedRoom.room.ylength);
                lvtrck_canvas.Children.Add(s_gui);
                List<UIElement> lst = new List<UIElement>();
                lst.Add(s_gui);
                uiElements.Add(s, lst);
            }            
        }

        
        internal void removeRoom(Room r)
        {
            lock(guilock)
            {
                RoomInfoGUI rig = roomToRoomInfoGUI[r];
                roomToRoomInfoGUI.Remove(r);
                roomlistpanel.Children.Remove(rig.container);
                dvcinfo_room.Items.Remove(r);
                if(selectedRoom != null && selectedRoom.room == r)
                {
                    if(roomlistpanel.Children.Count>0)
                        selectRoom(roomlistpanel.Children[0], null);
                }
            }
        }

        private void doColorIn(object sender, MouseEventArgs e)
        {
            ((Border)sender).Background = Brushes.LightGray;
        }
        private void doColorOut(object sender, MouseEventArgs e)
        {
            Border b = ((Border)sender);
            if(selectedRoom.container == b)
                b.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#e5e5e5");
            else
                b.Background = null;
        }
        internal void addRoom(Room room)
        {
            RoomInfoGUI ri = new RoomInfoGUI();
            ri.room = room;
            ri.container = new Border();
            ri.container.Tag = ri;
            Grid gr = new Grid();
            gr.Margin = new Thickness(10, 5, 10, 5);
            ri.container.Child = gr;
            ri.container.BorderThickness = new Thickness(0, 0, 0, 1);
            ri.container.BorderBrush = Brushes.Gray;
            ri.roomname = new TextBlock();
            ri.roomname.Width = roomlistpanel.Width - 20;
            ri.roomname.TextWrapping = TextWrapping.Wrap;
            ri.roomname.Text = room.roomName;
            ri.roomname.FontSize = 25;
            ri.roomname.Margin = new Thickness(0, 0, 0, 20);
            gr.Children.Add(ri.roomname);
            ri.peoplecount = new TextBlock();
            ri.peoplecount.Text = room.devicecount + " devices";
            ri.peoplecount.FontSize = 15;
            ri.peoplecount.Margin = new Thickness(0, 0, 0, 0);
            ri.peoplecount.HorizontalAlignment = HorizontalAlignment.Left;
            ri.peoplecount.VerticalAlignment = VerticalAlignment.Bottom;
            ri.peoplecount.Width = roomlistpanel.Width - 25;
            gr.Children.Add(ri.peoplecount);
            ri.stationcount = new TextBlock();
            ri.stationcount.Text = room.stationcount +" stations";
            ri.stationcount.FontSize = 15;
            ri.stationcount.Margin = new Thickness(0, 0, 15, 0);
            ri.stationcount.HorizontalAlignment = HorizontalAlignment.Right;
            ri.stationcount.VerticalAlignment = VerticalAlignment.Bottom;
            ri.container.MouseDown += selectRoom;
            ri.container.MouseEnter += doColorIn;
            ri.container.MouseLeave += doColorOut;
            if (room != Room.externRoom)
                gr.Children.Add(ri.stationcount);
            gr.Cursor = Cursors.Hand;
            
            lock(guilock)
            {
                roomlistpanel.Children.Add(ri.container);
                roomToRoomInfoGUI.Add(room, ri);
                dvcinfo_room.Items.Add(room);
            }
            
        }
        
        internal MainWindow(Context context)
        {
            ctx = context;
            ctx.guiPub.linkedwindow = this;
            InitializeComponent();
            int h = 0;
            int min = 0;
            while (h < 24)
            {
                while (min < 60)
                {
                    trackrlp_fromTime.Items.Add(h.ToString("00") + ":" + min.ToString("00"));
                    trackrlp_toTime.Items.Add(h.ToString("00") + ":" + min.ToString("00"));
                    dvcinfo_fromTime.Items.Add(h.ToString("00") + ":" + min.ToString("00"));
                    dvcinfo_toTime.Items.Add(h.ToString("00") + ":" + min.ToString("00"));
                    min += 15;
                }
                h += 1;
                min = 0;
            }
            trackrlp_fromTime.SelectedIndex = DateTime.Now.Hour * 4 + DateTime.Now.Minute / 15;
            trackrlp_toTime.SelectedIndex = (DateTime.Now.Hour * 4 + DateTime.Now.Minute / 15 + 1)%96;
            dvcinfo_fromTime.SelectedIndex = DateTime.Now.Hour * 4 + DateTime.Now.Minute / 15;
            dvcinfo_toTime.SelectedIndex = (DateTime.Now.Hour * 4 + DateTime.Now.Minute / 15 + 1) % 96;
            dvcinfo_room.Items.Add(Room.overallRoom);
            dvcinfo_room.SelectedIndex = 0;
            Style = (Style)FindResource(typeof(Window));
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (roomlistpanel.Children.Count > 0)
                selectRoom(roomlistpanel.Children[0], null);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ctx.kill();
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lock(guilock)
            {
                if (loadedreplay != null)
                    pause_MouseDown(null, null);

                if(selectedRoom!=null)
                {
                    if (tabControl.SelectedIndex == 2)
                        if (loadedstats==null&&selectedRoom.room!=Room.externRoom)
                            loadRoomStats(DateTime.Now.Month, DateTime.Now.Year, selectedRoom.room);
                }
            }
        }

        enum ReplayState { Loading, Loaded, NotLoaded};
        private void updateReplayControls(String message, ReplayState state)
        {
            trackrlp_time.Text = "";
            trackrlp_ReplayInfo.Text = message;
            if (state == ReplayState.Loading || state == ReplayState.NotLoaded)
            {
                if (loadedreplay != null)
                    pause_MouseDown(null, null);
                clearreloop();
                loadedreplay = null;
                trackrlp_controls.Visibility = Visibility.Hidden;
            }
            else
            {
                trackrlp_controls.Visibility = Visibility.Visible;
                trackrlp_speed.Text = "@" + 2 * loadedreplay.intersec + "x";
                trackrlp_time.Text = loadedreplay.at.ToString();
            }
            if (state == ReplayState.Loading)
                trackrlp_load.IsEnabled = false;
            else
                trackrlp_load.IsEnabled = true;
        }
        private void Rlp_load_Click(object sender, RoutedEventArgs e)
        {
            lock(guilock)
            {
                updateReplayControls("Loading...", ReplayState.Loading);
                DateTime? fromdate = trackrlp_fromDate.SelectedDate;
                DateTime? todate = trackrlp_toDate.SelectedDate;
                String fromtime = trackrlp_fromTime.Text;
                String totime = trackrlp_toTime.Text;
                if (!fromdate.HasValue || !todate.HasValue)
                {
                    updateReplayControls("Select start and end date of relooped period", ReplayState.NotLoaded);
                    return;
                }
                Regex timevalidation = new Regex("^\\d\\d?:\\d{2}$");
                if (!timevalidation.Match(fromtime).Success)
                {
                    updateReplayControls("Not a valid time: " + fromtime, ReplayState.NotLoaded);
                    return;
                }
                if (!timevalidation.Match(totime).Success)
                {
                    updateReplayControls("Not a valid time: " + totime, ReplayState.NotLoaded);
                    return;
                }
                DevicePosition[] data = ctx.databaseInt.loadDevicesPositions(selectedRoom.room.roomName, fromdate.Value, fromtime, todate.Value, totime);
                if(data.Length>0)
                {
                    loadedreplay = new Replay(data, selectedRoom.room);
                    updateReplayControls("Replay ready: " + fromdate.Value.ToString("dd / MM / yyyy ") + fromtime + " > " + todate.Value.ToString("dd / MM / yyyy ") + totime, ReplayState.Loaded);
                }
                else
                {
                    updateReplayControls("No event in the selected timelapse",ReplayState.NotLoaded);
                }

            }
        }

        private void animationstep(object obj, EventArgs ev)
        {
            lock(guilock)
            {
                System.Diagnostics.Debug.Print(loadedreplay.at.ToString());
                if (!loadedreplay.playing || (loadedreplay.indexupto==loadedreplay.replaydata.Length-1 && loadedreplay.replaydata[loadedreplay.indexupto].timestamp<loadedreplay.at))
                    return;
                DevicePosition currelem = loadedreplay.replaydata[loadedreplay.indexupto];
                while(loadedreplay.indexupto<loadedreplay.replaydata.Length)
                {
                    currelem = loadedreplay.replaydata[loadedreplay.indexupto];
                    if (currelem.timestamp >= loadedreplay.at.AddSeconds(loadedreplay.intersec))
                        break;
                    PositionTools.Position pos = new PositionTools.Position(currelem.xpos,currelem.ypos, loadedreplay.refroom);
                    EventType ev2 = EventType.Update;
                    if (currelem.prexpos < 0)
                        ev2 = EventType.MoveIn;
                    else if (currelem.moveout)
                        ev2 = EventType.MoveOut;
                    drawDevice(currelem.identifier, pos, ev2, trackrlp_canvas, trackrlp_devlist_panel, loadedreplay.uiElements, null);
                    loadedreplay.indexupto += 1;
                }
                loadedreplay.at = loadedreplay.at.AddSeconds(loadedreplay.intersec);
                if (loadedreplay.indexupto >= loadedreplay.replaydata.Length)
                {
                    loadedreplay.indexupto--;
                    pause_MouseDown(null, null);
                }
                trackrlp_time.Text = loadedreplay.at.ToString();
            }
        }

        private void back_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lock(guilock)
            {
                DateTime newtime = loadedreplay.at.AddSeconds(-10);
                System.Diagnostics.Debug.Print(loadedreplay.indexupto.ToString());
                DevicePosition currelem = loadedreplay.replaydata[loadedreplay.indexupto];
                while (loadedreplay.indexupto>=0 && currelem.timestamp>=newtime)
                {
                    PositionTools.Position pos = new PositionTools.Position(currelem.prexpos, currelem.preypos, loadedreplay.refroom);
                    EventType ev2 = EventType.Update;
                    if (currelem.prexpos < 0)
                        ev2 = EventType.MoveOut;
                    else if (currelem.moveout)
                        ev2 = EventType.MoveIn;
                    drawDevice(currelem.identifier, pos, ev2, trackrlp_canvas, trackrlp_devlist_panel, loadedreplay.uiElements, null);
                    loadedreplay.indexupto--;
                    if (loadedreplay.indexupto >= 0)
                        currelem = loadedreplay.replaydata[loadedreplay.indexupto];
                }
                if (loadedreplay.indexupto < 0)
                    loadedreplay.indexupto = 0;
                loadedreplay.at = newtime;
                trackrlp_time.Text = loadedreplay.at.ToString();
            }
        }

        private void slower_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lock (guilock)
            {
                if (loadedreplay.intersec > 0.1)
                    loadedreplay.intersec = loadedreplay.intersec / 2;
                trackrlp_speed.Text = "@"+2 * loadedreplay.intersec + "x";
            }
        }

        private void pause_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lock(guilock)
            {
                if(loadedreplay.playing)
                    loadedreplay.timer.Stop();
                loadedreplay.playing = false;
            }
        }

        private void play_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lock(guilock)
            {
                if (loadedreplay.playing)
                    return;
                loadedreplay.playing = true;
                trackrlp_speed.Text = "@"+2 * loadedreplay.intersec + "x";
                loadedreplay.timer = new System.Windows.Threading.DispatcherTimer(new TimeSpan(0, 0, 0, 0,500), System.Windows.Threading.DispatcherPriority.Normal, new EventHandler(animationstep), Application.Current.Dispatcher);
                loadedreplay.timer.Start();
                animationstep(null, null);
            }
        }

        private void reset_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lock(guilock)
            {
                pause_MouseDown(null, null);
                loadedreplay.indexupto = 0;
                loadedreplay.at = loadedreplay.startingpoint;
                loadedreplay.uiElements.Clear();
                clearreloop();
                trackrlp_speed.Text = "@" + 2 * loadedreplay.intersec + "x";
                trackrlp_time.Text = loadedreplay.at.ToString();

            }
        }
        private void clearreloop()
        {
            lock(guilock)
            {
                trackrlp_time.Text = "";
                trackrlp_speed.Text = "";
                trackrlp_canvas.Children.Clear();
                trackrlp_devlist_panel.Children.Clear();
            }
        }
        private void faster_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lock(guilock)
            {
                if(loadedreplay.intersec<16)
                    loadedreplay.intersec = loadedreplay.intersec * 2;
                trackrlp_speed.Text = "@"+2 * loadedreplay.intersec + "x";
            }
        }

        private void forward_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lock(guilock)
            {
                DevicePosition currelem = loadedreplay.replaydata[loadedreplay.indexupto];
                DateTime newtime = loadedreplay.at.AddSeconds(10);
                while (loadedreplay.indexupto < loadedreplay.replaydata.Length)
                {
                    currelem = loadedreplay.replaydata[loadedreplay.indexupto];
                    if (currelem.timestamp >= newtime)
                        break;
                    PositionTools.Position pos = new PositionTools.Position(currelem.xpos, currelem.ypos, loadedreplay.refroom);
                    EventType ev2 = EventType.Update;
                    if (currelem.prexpos < 0)
                        ev2 = EventType.MoveIn;
                    else if (currelem.moveout)
                        ev2 = EventType.MoveOut;
                    drawDevice(currelem.identifier, pos, ev2, trackrlp_canvas, trackrlp_devlist_panel, loadedreplay.uiElements, null);
                    loadedreplay.indexupto += 1;
                }
                loadedreplay.at = newtime;
                if (loadedreplay.indexupto >= loadedreplay.replaydata.Length)
                {
                    loadedreplay.indexupto--;
                    pause_MouseDown(null, null);
                }
                trackrlp_time.Text = loadedreplay.at.ToString();
            }
        }
        private void loadRoomStats(int month, int year, Room room)
        {
            Stats stats = new Stats(month,year,room);
            BitmapSource hmap = null;
            stats.maxperday = ctx.databaseInt.loadMaxDevicesDay(month, year, room.roomName);
            if(stats.maxperday!=null)
            {
                stats.avgperhour = ctx.databaseInt.loadAvgDevicesTime(month, year, room.roomName);
                stats.macs = ctx.databaseInt.loadFrequentMacs(month, year, room.roomName);
                if (room != Room.externRoom)
                {
                    stats.heatmaps = ctx.databaseInt.loadHeathmaps(null, room.roomName, room.xlength, room.ylength, month, year);
                    hmap = Graphics.createheatmap(stats.heatmaps[stats.selectedday]);
                }
            }
            updateRoomStats(true, stats,hmap);
        }
        private void updateRoomStats(Boolean full, Stats stats, BitmapSource hmap)
        {
            lock(guilock)
            {
                if (stats.refroom != selectedRoom.room) //user changed room while loading data
                {
                    return;
                }
                loadedstats = stats;
                rmstats_heatmap.Source = hmap;
                rmstats_avgtime.Children.Clear();
                rmstats_frequentmacs.Children.Clear();
                if (full)
                {
                    rmstats_maxday.Children.Clear();
                    if (stats.maxperday != null)
                    {
                        rmstats_daylabel.Text = "Max number of devices per day in " + months[stats.selectedmonth-1] + " " + stats.selectedyear;
                        int initial = (int)new DateTime(stats.selectedyear,stats.selectedmonth,1).DayOfWeek;
                        Graphics.drawHistogram(rmstats_maxday, stats.maxperday, (double d, int i, object o) => { return (i + 1) + (String)o + d.ToString("G2"); }, "/" + stats.selectedmonth + " - ", dayStatSelect, weekhistcolors, weekhistcolors, initial,(double d, int i, object o)=> { return i; },null);
                    }
                    else
                        rmstats_daylabel.Text = "No data for " + months[stats.selectedmonth-1] + " " + stats.selectedyear;
                    
                }
                if (stats.avgperhour != null)
                {
                    rmstats_timelabel.Text = "Avg number of devices per hour " + (stats.selectedday==0?"in ":"the "+stats.selectedday+" ") + months[stats.selectedmonth - 1] + " " + stats.selectedyear;
                    Graphics.drawHistogram(rmstats_avgtime, stats.avgperhour[stats.selectedday], (double d, int i, object o) => { return "h" + i + " - " + d.ToString("G2"); }, null, null, timehistcolors, timehistcolors, 0, null, null);
                    for (int d=0;d<stats.macs.GetLength(1);d++)
                    {
                        String mac = stats.macs[stats.selectedday, d];
                        if(mac!=null)
                        {
                            TextBlock tb = new TextBlock();
                            tb.Text = mac;
                            tb.FontSize = 16;
                            tb.Cursor = Cursors.Hand;
                            tb.Tag = mac;
                            tb.MouseDown += opendeviceinfo;
                            rmstats_frequentmacs.Children.Add(tb);
                        }
                    }
                }
                else
                    rmstats_timelabel.Text = "No data for " + months[stats.selectedmonth - 1] + " " + stats.selectedyear;
            }
        }

        
        private void dayStatSelect(object sender, MouseButtonEventArgs e)
        {
            int preselected = loadedstats.selectedday;
            loadedstats.selectedday = (int)((Rectangle)sender).Tag;
            if (preselected == loadedstats.selectedday)
                loadedstats.selectedday = 0;
            if (preselected > 0)
            {
                ((Rectangle)rmstats_maxday.Children[preselected]).Stroke = ((Rectangle)rmstats_maxday.Children[preselected > 8 ? preselected - 7 : preselected + 7]).Stroke;
                ((Rectangle)rmstats_maxday.Children[preselected]).Fill = ((Rectangle)rmstats_maxday.Children[preselected > 8 ? preselected - 7 : preselected + 7]).Fill;
            }
            if(loadedstats.selectedday>0)
            {
                ((Rectangle)rmstats_maxday.Children[loadedstats.selectedday]).Stroke = Brushes.LightGray;
                ((Rectangle)rmstats_maxday.Children[loadedstats.selectedday]).Fill = Brushes.LightGray;
            }
            BitmapSource hmap = null;
            if(loadedstats.refroom!=Room.externRoom)
                hmap = Graphics.createheatmap(loadedstats.heatmaps[loadedstats.selectedday]);
            updateRoomStats(false, loadedstats, hmap);
        }
        private void rmstats_premonth_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (loadedstats == null)
                return;
            rmstats_daylabel.Text = "Loading data...";
            rmstats_timelabel.Text = "Loading data...";
            int newmonth = loadedstats.selectedmonth - 1;
            int newyear = loadedstats.selectedyear;
            if(newmonth<1)
            {
                newmonth = 12;
                newyear = newyear - 1;
            }
            loadRoomStats(newmonth,newyear,selectedRoom.room);
        }

        private void rmstats_nextmonth_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (loadedstats == null)
                return;
            rmstats_daylabel.Text = "Loading data...";
            rmstats_timelabel.Text = "Loading data...";
            int newmonth = loadedstats.selectedmonth +1;
            int newyear = loadedstats.selectedyear;
            if (newmonth > 12)
            {
                newmonth = 1;
                newyear = newyear + 1;
            }
            loadRoomStats(newmonth, newyear,selectedRoom.room);
        }

        private void resetRmStats(Boolean resetMacs)
        {
            lock(guilock)
            {
                if(resetMacs)
                    rmstats_frequentmacs.Children.Clear();
                rmstats_heatmap.Source = null;
                rmstats_maxday.Children.Clear();
                rmstats_avgtime.Children.Clear();
                rmstats_frequentmacs.Children.Clear();
                rmstats_daylabel.Text = "Loading data...";
                rmstats_timelabel.Text = "Loading data...";
            }
        }
        private void opendeviceinfo(object sender, RoutedEventArgs e)
        {
            String mac = (String)((FrameworkElement)sender).Tag;
            dvcinfo_idtextbox.Text = mac;
            dvcinfo_search_Click(null, null);
            dvinfo.RaiseEvent(e);
        }
        private void dvcinfo_search_Click(object sender, RoutedEventArgs e)
        {
            dvcinfo_deviceid.Text = "Loading data...";
            dvcinfo_load.IsEnabled = false;
            loadDeviceStats(dvcinfo_idtextbox.Text);
        }
        private void loadDeviceStats(String id)
        {
            clearDeviceInfo();
            DeviceInfo di = ctx.databaseInt.loadDeviceInfo(id);
            if(di==null)
            {
                dvcinfo_deviceid.Text = "Device not found.";
                dvcinfo_load.IsEnabled = false;
                return;
            }
            dvcinfo_deviceid.Text = id;
            dvcinfo_firstdetected.Text = di.FirstSeen.ToString("dd/MM/yyyy HH:mm");
            dvcinfo_lastdetected.Text = di.LastSeen.ToString("dd/MM/yyyy HH:mm");
            dvcinfo_load.IsEnabled = true;
            foreach (String ssid in di.ssids)
            {
                TextBlock tb = new TextBlock();
                tb.Text = ssid;
                tb.FontSize = 16;
                dvcinfo_ssids.Children.Add(tb);
            }
        }

        private void clearDeviceInfo()
        {
            dvcinfo_load.IsEnabled = false;
            dvcinfo_lastdetected.Text = "";
            dvcinfo_firstdetected.Text = "";
            dvcinfo_ssids.Children.Clear();
            dvcinfo_loadstatus.Content = "";
            dvcinfo_extralabel.Content = "";
            dvcinfo_heatmap.Visibility = Visibility.Hidden;
            dvcinfo_heatmap.Source = null;
            dvcinfo_roomsmap.Children.Clear();
            dvcinfo_roomsmap.Visibility = Visibility.Hidden;
        }

        private void dvcinfo_load_Click(object sender, RoutedEventArgs e)
        {
            clearDeviceInfo();
            dvcinfo_loadstatus.Content = "Loading...";
            dvcinfo_load.IsEnabled = false;
            DateTime? fromdate = dvcinfo_fromDate.SelectedDate;
            DateTime? todate = dvcinfo_toDate.SelectedDate;
            String fromtime = dvcinfo_fromTime.Text;
            String totime = dvcinfo_toTime.Text;
            if (!fromdate.HasValue || !todate.HasValue)
            {
                dvcinfo_loadstatus.Content = "Select start and end date of relooped period";
                dvcinfo_load.IsEnabled = true;
                return;
            }
            Regex timevalidation = new Regex("^\\d\\d?:\\d{2}$");
            if (!timevalidation.Match(fromtime).Success)
            {
                dvcinfo_loadstatus.Content = "Not a valid time: " + fromtime;
                dvcinfo_load.IsEnabled = true;
                return;
            }
            if (!timevalidation.Match(totime).Success)
            {
                dvcinfo_loadstatus.Content = "Not a valid time: " + totime;
                dvcinfo_load.IsEnabled = true;
                return;
            }
            Room dvcstatroom = ((Room)dvcinfo_room.SelectedItem);
            if (dvcstatroom == Room.overallRoom)
                dvcinfo_roomsmap.Visibility = Visibility.Visible;
            else if (dvcstatroom != Room.externRoom)
                dvcinfo_heatmap.Visibility = Visibility.Visible;
            DeviceStats ds = ctx.databaseInt.loadDeviceStats(fromdate.Value, fromtime, todate.Value, totime, dvcinfo_idtextbox.Text, dvcstatroom.roomName, dvcstatroom == Room.overallRoom, dvcstatroom != Room.externRoom && dvcstatroom != Room.overallRoom, dvcstatroom.xlength, dvcstatroom.ylength);
            if(ds==null)
            {
                dvcinfo_loadstatus.Content = "Database error";
                dvcinfo_load.IsEnabled = true;
                return;
            }
            if (dvcstatroom != Room.externRoom && dvcstatroom != Room.overallRoom)
                dvcinfo_heatmap.Source = Graphics.createheatmap(ds.heatmap);
            else if(dvcstatroom==Room.overallRoom)
            {
                int tot = ds.roommap["__OVERALL__"];
                ds.roommap.Remove("__OVERALL__");
                foreach(string rm in ds.roommap.Keys)
                {
                    double count = (double)ds.roommap[rm] * 100.0 / (double)tot;
                    TextBlock tb = new TextBlock();
                    tb.Text = rm + ": " + count + "%";
                    tb.FontSize = 16;
                    dvcinfo_roomsmap.Children.Add(tb);
                }
            }
            Graphics.drawHistogram(dvcinfo_dayspresent, ds.timeperday, (double val, int i, object data) => { return ""; }, fromdate, null, weekhistcolors, weekhistcolors, (int)fromdate.Value.DayOfWeek,null,null);
            Graphics.drawHistogram(dvcinfo_hourspresent, ds.pingsperhour, (double val, int i, object data) => { return ""; }, null, null, timehistcolors, timehistcolors, 0,null,null);
            dvcinfo_loadstatus.Content = "";
        }

        private void dvcinfo_idtextbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                dvcinfo_search_Click(null,null);
        }
    }

       
}
