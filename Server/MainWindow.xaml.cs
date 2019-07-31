using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace Panopticon
{
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    partial class MainWindow : Window
    {
        Context ctx;
        Object guilock = new Object();
        RoomInfoGUI selected = null;
        Dictionary<Room, RoomInfoGUI> roomToRoomInfoGUI = new Dictionary<Room, RoomInfoGUI>();
        Dictionary<object, List<UIElement>> uiElements = new Dictionary<object, List<UIElement>>();
        class RoomInfoGUI
        {
            internal Border container;
            internal TextBlock roomname;
            internal TextBlock peoplecount;
            internal TextBlock stationcount;
            internal Room room;
        }

        internal void updateDevicePosition(Device d, PositionTools.Position lastPosition)
        {
            drawDevice(d, lastPosition);
        }
        
        private void drawDevice(Device d, PositionTools.Position lastPosition)
        {
            
            lock(guilock)
            {
                List<UIElement> d_gui;
                if(lastPosition.room==ctx.guiPub.linkedroom)
                {
                    if(!uiElements.TryGetValue(d,out d_gui))
                    {
                        d_gui = new List<UIElement>();
                        Ellipse d_gui_e = new Ellipse();
                        d_gui_e.Height = ctx.guiPub.linkedroom.xlength / 2;
                        d_gui_e.Width = ctx.guiPub.linkedroom.ylength / 2;
                        d_gui_e.ToolTip = "Device " + d.identifier;
                        d_gui_e.Tag = d;
                        d_gui_e.Fill = Utilities.FancyColorCreator.randomBrush(d.identifier.GetHashCode());
                        if (lastPosition.uncertainity == double.MaxValue)
                            d_gui_e.Fill.Opacity = 0;
                        d_gui_e.Stroke = d_gui_e.Fill;
                        d_gui_e.Cursor = Cursors.Hand;
                        d_gui.Add(d_gui_e);
                        TextBlock lab = new TextBlock();
                        lab.Text = "Device " + d.identifier;
                        lab.FontSize = 16;
                        lab.Foreground = d_gui_e.Fill;
                        lab.Tag = d;
                        lab.Cursor = Cursors.Hand;
                        d_gui.Add(lab);
                        lvtrck_devlist_panel.Children.Add(lab);
                        lvtrck_canvas.Children.Add(d_gui_e);
                        uiElements.Add(d, d_gui);
                    }
                    Canvas.SetLeft(d_gui[0], lastPosition.X * lvtrck_canvas.Width / ctx.guiPub.linkedroom.xlength);
                    Canvas.SetTop(d_gui[0], lastPosition.Y * lvtrck_canvas.Height / ctx.guiPub.linkedroom.ylength);
                }
                else
                {
                    if (uiElements.TryGetValue(d, out d_gui))
                    {
                        lvtrck_canvas.Children.Remove(d_gui[0]);
                        lvtrck_devlist_panel.Children.Remove(d_gui[1]);
                        uiElements.Remove(d);
                    }
                    //else ignore
                }
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
                if(ctx.guiPub.linkedroom==r)
                    lvtrck_people.Content = stat + " devices";
            }
        }

        internal void updateTenMinutesDeviceCount(Room r, double stat)
        {
        }

        private void selectRoom(object sender,MouseButtonEventArgs e)
        {
            lock(guilock) //avoid pre-emption in the middle of redrawing
            {
                if (selected != null)
                    selected.container.Background = null;
                selected = (RoomInfoGUI)((Border)sender).Tag;
                ctx.guiPub.linkedroom = selected.room;
                lvtrck_roomname.Content = selected.room.roomName;
                lvtrck_stations.Content = selected.stationcount.Text;
                lvtrck_people.Content = selected.peoplecount.Text;
                if (lvtrck.Visibility == Visibility.Hidden)
                {
                    lvtrck.Visibility = Visibility.Visible;
                    trackrlp.Visibility = Visibility.Visible;
                    rmstats.Visibility = Visibility.Visible;
                }
                selected.container.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#e5e5e5");
                lvtrck_canvas.Children.Clear();
                lvtrck_devlist_panel.Children.Clear();
                uiElements.Clear();
                lvtrck_viewbox.Child = null;
                lvtrck_border.Width = selected.room.xlength * 20.5;
                lvtrck_border.Height = selected.room.ylength * 20.5;
                lvtrck_canvas.Width = selected.room.xlength*20;
                lvtrck_canvas.Height = selected.room.ylength*20;
                lvtrck_viewbox.Child = lvtrck_border;
                foreach (Station s in ctx.guiPub.linkedroom.getStations())
                    drawStation(s);
                foreach (Device d in ctx.guiPub.linkedroom.getDevices())
                    drawDevice(d, d.lastPosition);
            }
            
            //load room info
        }

        internal void updateStation(Room r, Station s, Publisher.EventType e)
        {
            lock (guilock)
            {
                RoomInfoGUI rig = roomToRoomInfoGUI[r];
                int stationcount = r.stationcount;
                if (ctx.guiPub.linkedroom == r)
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
            s_gui.Height = ctx.guiPub.linkedroom.xlength / 3;
            s_gui.Width = ctx.guiPub.linkedroom.ylength / 3;
            s_gui.ToolTip = "Station " + s.NameMAC;
            s_gui.Tag = s;
            s_gui.Fill = Brushes.Red;
            s_gui.Stroke = Brushes.Red;
            s_gui.Cursor = Cursors.Hand;
            lock(guilock)
            {
                Canvas.SetLeft(s_gui, s.location.X * lvtrck_canvas.Width / ctx.guiPub.linkedroom.xlength);
                Canvas.SetTop(s_gui, s.location.Y * lvtrck_canvas.Height / ctx.guiPub.linkedroom.ylength);
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
                if(ctx.guiPub.linkedroom == r)
                {
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
            if(selected.container == b)
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
            if (room != Room.externRoom)
            {
                ri.container.MouseDown += selectRoom;
                ri.container.MouseEnter += doColorIn;
                ri.container.MouseLeave += doColorOut;
                gr.Children.Add(ri.stationcount);
                gr.Cursor = Cursors.Hand;
            }
            else
            {
                ri.container.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#dddddd");
            }
            roomlistpanel.Children.Add(ri.container);
            roomToRoomInfoGUI.Add(room, ri);
        }
        
        internal MainWindow(Context context)
        {
            ctx = context;
            ctx.guiPub.linkedwindow = this;
            InitializeComponent();
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
                if(ctx.guiPub.linkedroom!=null)
                {
                    
                }
            }
/*
            StationAdder sa1 = new StationAdder();
			sa1.Show();

            
            Console.WriteLine("Programma avviato");
            //Connection.StartConnection();

            //Console.ReadLine();
            
            Thread socketListener = new Thread(new ThreadStart(Connection.StartConnection));
            socketListener.Start();
            double[] x = { dist2RSSI(0), dist2RSSI(10), dist2RSSI(20), dist2RSSI(30), dist2RSSI(40)};
            double[] y = { 0, 10,20, 30, 40 };
            Context ctx = new Context();
            Thread backgroundProcessManager = new Thread(new ThreadStart(ctx.orchestrate));
            backgroundProcessManager.Start();
			
            //TODO FEDE: aprire finestra creazione stanza
            PositionTools.Room r = ctx.createRoom("TestRoom", 25, 25);
            StationHandler sh1 = null, sh2 = null, sh3 = null;
            //funzione che non so dove sarà chiamata
            //sh1 = new StationHandler(socket);
            Station s1=ctx.createStation(r, "0.0", 0, 0,sh1);
            PositionTools.calibrateInterpolators(y, x, s1);
            Station s2 = ctx.createStation(r, "25.0", 25, 0,sh2);
            PositionTools.calibrateInterpolators(y, x, s2);
            Station s3 = ctx.createStation(r, "0.25", 0, 25,sh3);
            PositionTools.calibrateInterpolators(y, x, s3);
            
            //formazione di un oggetto Packet
            Packet p = new Packet("abcde928", "Alice33Test", DateTime.Now, "", "", 0);
            p.received(s1, dist2RSSI(12.5));
            p.received(s2, dist2RSSI(12.5));
            p.received(s3, dist2RSSI(Math.Sqrt(12.5 * 12.5 + 25 * 25)));
            ctx.getAnalyzer().sendToAnalysisQueue(p);

            Thread.Sleep(1500);

            p = new Packet("abcde928e", "Fastweb25Test", DateTime.Now, "", "", 0);
            p.received(s1, dist2RSSI(25));
            p.received(s2, dist2RSSI(Math.Sqrt(25 * 25 + 25 * 25)));
            p.received(s3, dist2RSSI(0));
            ctx.getAnalyzer().sendToAnalysisQueue(p);

            Thread.Sleep(12000);

            //ctx.switchCalibration(true, r);

            p = new Packet("abcde928", "Fastweb25Test", DateTime.Now, "", "", 0);
            p.received(s1, dist2RSSI(25));
            p.received(s2, dist2RSSI(Math.Sqrt(25 * 25 + 25 * 25)));
            p.received(s3, dist2RSSI(0));
            ctx.getAnalyzer().sendToAnalysisQueue(p);

            //ctx.switchCalibration(false,r);

            p = new Packet("abcde929", "Polito", DateTime.Now, "", "", 0);
            p.received(s1, dist2RSSI(0));
            p.received(s2, dist2RSSI(25));
            p.received(s3, dist2RSSI(25));
            ctx.getAnalyzer().sendToAnalysisQueue(p);

            Thread.Sleep(8000);
            ctx.getAnalyzer().kill();
            */

        }
    }

       
}
