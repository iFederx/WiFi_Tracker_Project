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
        Boolean statsLoaded = false;
        Replay loadedreplay = null;
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

            public Replay(DevicePosition[] devicePosition)
            {
                replaydata = devicePosition;
                startingpoint = replaydata[0].timestamp;
                at = startingpoint;
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
                if(ev == EventType.Appear || ev ==EventType.MoveIn || ev == EventType.Update)
                {
                    if(!uielem.TryGetValue(deviceIdentifier, out d_gui))
                    {
                        d_gui = new List<UIElement>();
                        Ellipse d_gui_e = new Ellipse();
                        d_gui_e.Height = selectedRoom.room.xlength / 2;
                        d_gui_e.Width = selectedRoom.room.ylength / 2;
                        d_gui_e.ToolTip = "Device " + deviceIdentifier;
                        d_gui_e.Tag = deviceIdentifier;
                        d_gui_e.Fill = Utilities.FancyColorCreator.randomBrush(deviceIdentifier.GetHashCode());
                        if (lastPosition.uncertainity == double.MaxValue)
                            d_gui_e.Fill.Opacity = 0;
                        d_gui_e.Stroke = d_gui_e.Fill;
                        d_gui_e.Cursor = Cursors.Hand;
                        d_gui.Add(d_gui_e);
                        TextBlock lab = new TextBlock();
                        lab.Text = "Device " + deviceIdentifier;
                        lab.FontSize = 16;
                        lab.Foreground = d_gui_e.Fill;
                        lab.Tag = deviceIdentifier;
                        lab.Cursor = Cursors.Hand;
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
            if (rg.statsqueue.Count > 144)
                rg.statsqueue.Dequeue();
            drawStats(r);
        }

        private void drawStats(Room r)
        {
            lock(guilock)
            {
                if (selectedRoom == null || r != selectedRoom.room)
                    return;
                occup_histo.Children.Clear();
                RoomInfoGUI rg = roomToRoomInfoGUI[r];
                double max = 1;
                int margin = 0;
                foreach(double d in rg.statsqueue)
                {
                    Rectangle re = new Rectangle();
                    re.Height = 10 * d;
                    re.Width = 5;
                    re.Fill = Brushes.Black;
                    re.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#0f0f0f");
                    if (d > max)
                        max = d;
                    re.VerticalAlignment = VerticalAlignment.Bottom;
                    Canvas.SetLeft(re, margin);
                    Canvas.SetBottom(re, 0);
                    occup_histo.Children.Add(re);
                    re.ToolTip = DateTime.Now.ToString("dd/MM HH:mm") + " - " + d.ToString("G2");
                    margin += 5;
                }
                occup_histo.Height = 10 * max;

            }
        }

        private void selectRoom(object sender,MouseButtonEventArgs e)
        {
            lock(guilock) //avoid pre-emption in the middle of redrawing
            {
                if (selectedRoom != null)
                    selectedRoom.container.Background = null;
                statsLoaded = false;
                updateReplayControls("No loaded replay.", ReplayState.NotLoaded);
                selectedRoom = (RoomInfoGUI)((Border)sender).Tag;
                ctx.guiPub.linkedroom = selectedRoom.room;
                lvtrck_roomname.Content = selectedRoom.room.roomName;
                trackrlp_roomname.Content = selectedRoom.room.roomName;
                lvtrck_stations.Content = selectedRoom.stationcount.Text;
                lvtrck_people.Content = selectedRoom.peoplecount.Text;
                lvtrck.IsEnabled = selectedRoom.room!=Room.externRoom?true:false;
                trackrlp.IsEnabled = selectedRoom.room != Room.externRoom ? true : false;
                tabControl.SelectedIndex = selectedRoom.room != Room.externRoom ? 0 : 2;
                trackrlp.Visibility = Visibility.Visible;
                rmstats.Visibility = Visibility.Visible;
                selectedRoom.container.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#e5e5e5");
                lvtrck_canvas.Children.Clear();
                lvtrck_devlist_panel.Children.Clear();
                uiElements.Clear();
                lvtrck_viewbox.Child = null;
                lvtrck_border.Width = selectedRoom.room.xlength * 20.5;
                lvtrck_border.Height = selectedRoom.room.ylength * 20.5;
                lvtrck_canvas.Width = selectedRoom.room.xlength*20;
                lvtrck_canvas.Height = selectedRoom.room.ylength*20;
                lvtrck_viewbox.Child = lvtrck_border;
                trackrlp_viewbox.Child = null;
                trackrlp_border.Width = selectedRoom.room.xlength * 20.5;
                trackrlp_border.Height = selectedRoom.room.ylength * 20.5;
                trackrlp_canvas.Width = selectedRoom.room.xlength * 20;
                trackrlp_canvas.Height = selectedRoom.room.ylength * 20;
                trackrlp_viewbox.Child = trackrlp_border;
                foreach (Station s in selectedRoom.room.getStations())
                    drawStation(s);
                foreach (Device d in selectedRoom.room.getDevices())
                    drawDevice(d.identifier, d.lastPosition, EventType.MoveIn, lvtrck_canvas, lvtrck_devlist_panel, uiElements, lvtrck_people);
                drawStats(selectedRoom.room);
            }
            
            //load room info
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
            }
            
        }
        private void loadRoomStats()
        {

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
                    min += 15;
                }
                h += 1;
                min = 0;
            }
            trackrlp_fromTime.SelectedIndex = DateTime.Now.Hour * 4 + DateTime.Now.Minute / 15;
            trackrlp_toTime.SelectedIndex = (DateTime.Now.Hour * 4 + DateTime.Now.Minute / 15 + 1)%96;
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
                        if (!statsLoaded)
                            loadRoomStats();
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
                    if (loadedreplay.playing)
                        loadedreplay.timer.Stop();
                clearreloop();
                loadedreplay = null;
                trackrlp_controls.Visibility = Visibility.Hidden;
            }
            else
                trackrlp_controls.Visibility = Visibility.Visible;
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
                    updateReplayControls("Not a valid time: " + totime, ReplayState.Loading);
                    return;
                }
                DevicePosition[] data = ctx.databaseInt.loadDevicesPositions(selectedRoom.room.roomName, fromdate.Value, fromtime, todate.Value, totime);
                if(data.Length>0)
                {
                    loadedreplay = new Replay(data);
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
                if (!loadedreplay.playing)
                    return;
                DevicePosition currelem = loadedreplay.replaydata[loadedreplay.indexupto];
                while(loadedreplay.indexupto<loadedreplay.replaydata.Length)
                {
                    currelem = loadedreplay.replaydata[loadedreplay.indexupto];
                    if (currelem.timestamp >= loadedreplay.at.AddSeconds(loadedreplay.intersec))
                        break;
                    PositionTools.Position pos = new PositionTools.Position(currelem.xpos,currelem.ypos,null);
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
                    reset_MouseDown(null, null);
                trackrlp_time.Text = loadedreplay.at.ToString();
            }
        }

        private void back_MouseDown(object sender, MouseButtonEventArgs e)
        {

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
                loadedreplay.playing = false;
                loadedreplay.timer.Stop();
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
                animationstep(null, null);
                loadedreplay.timer = new System.Windows.Threading.DispatcherTimer(new TimeSpan(0, 0, 0, 0,500), System.Windows.Threading.DispatcherPriority.Normal, new EventHandler(animationstep), Application.Current.Dispatcher);
                loadedreplay.timer.Start();
            }
        }

        private void reset_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lock(guilock)
            {
                if(loadedreplay.playing)
                    loadedreplay.timer.Stop();
                loadedreplay.playing = false;
                loadedreplay.indexupto = 0;
                loadedreplay.at = loadedreplay.startingpoint;
                loadedreplay.uiElements.Clear();
                clearreloop();
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

        }
    }

       
}
