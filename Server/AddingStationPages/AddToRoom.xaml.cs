using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace Panopticon.AddingStationPages
{
    /// <summary>
    /// Logica di interazione per AddToRoom.xaml
    /// </summary>
    public partial class AddToRoom : Page
    {
		private Room room;
		Context ctx;
		double meterX=0, meterY=0;
		StationHandler handler = null;
		static int maxRoomDimension = 300; //dpi
		static int minRoomDimension = 200; //dpi
		static int maxN = 20;

		public AddToRoom()
        {
            InitializeComponent();
        }

		internal AddToRoom(Context _ctx, Room _room, StationHandler _handler)
		{
			InitializeComponent();
			room = _room;
			ctx = _ctx;
			handler = _handler;
			Label_RoomName.Content = room.roomName;
			handler.switchLedBlink(true);
			AdjustRoomSize(room);
			
			int i = FindWindowByMAC(handler.macAddress, maxN);
			if (i != maxN)
			{
				Application.Current.Windows[i].MinWidth = GridRoom.Width + 100;
				Application.Current.Windows[i].MinHeight = GridRoom.Height + 200;
			}
			//TODO: ci starebbe vedere le Station già presenti, magari in blu
			
		}

		private void AdjustRoomSize(Room room)
		{
			double maxVal = Math.Max(room.xlength, room.ylength);
			double minVal = Math.Min(room.xlength, room.ylength);
			if (maxVal > maxRoomDimension) //una delle due dimensioni supera la massima consentita
			{
				GridRoom.Height = maxRoomDimension;
				GridRoom.Width = room.xlength / room.ylength * GridRoom.Height;
			}
			else if (minVal < minRoomDimension) //una delle due dimensioni è troppo piccola
			{
				GridRoom.Height = maxRoomDimension;
				GridRoom.Width = room.xlength / room.ylength * GridRoom.Height;
			}
			else
			{
				GridRoom.Width = room.xlength;
				GridRoom.Height = room.ylength;
			}
		}

		internal int FindWindowByMAC(string _macAddress, int _maxN)
		{
			int i = 0;
			foreach (Window w in Application.Current.Windows)
			{
				if (i < _maxN)
					if (String.Compare(w.Title, ("Add a new Station (" + _macAddress + ")")) == 0)
					{
						return i;
					}
					else i++;
				else
					break;
			}
			return _maxN;
		}

		private void Grid_MouseMove(object sender, MouseEventArgs e)
		{
			//room.xlenght è la dimensione in cm della stanza
			//GridRoom.Width è la dimensione in dpi della stanza

			double meterX = Mouse.GetPosition(this.GridRoom).X / this.GridRoom.Width * room.xlength;
			double meterY = Mouse.GetPosition(this.GridRoom).Y / this.GridRoom.Height * room.ylength;
			if (meterX < 0) meterX = 0;
			if (meterY < 0) meterY = 0;
			if (meterX > room.xlength) meterX = room.xlength;
			if (meterY > room.ylength) meterY = room.ylength;
			xLabel.Content = meterX.ToString("X: 0.## cm");
			yLabel.Content = meterY.ToString("Y: 0.## cm");

		}

		private void Grid_NewPosition(object sender, MouseButtonEventArgs e)
		{
			meterX = Mouse.GetPosition(this.GridRoom).X / this.GridRoom.Width * room.xlength;
			meterY = Mouse.GetPosition(this.GridRoom).Y / this.GridRoom.Height * room.ylength;
			if (meterX < 0) meterX = 0;
			if (meterY < 0) meterY = 0;
			if (meterX > room.xlength) meterX = room.xlength;
			if (meterY > room.ylength) meterY = room.ylength;
			LastX.Content = meterX.ToString("Last X: 0.## cm");
			LastY.Content = meterY.ToString("Last Y: 0.## cm");
			Button_ConfirmPosition.IsEnabled = true;
			Ellipse_StationPosition.SetValue(Canvas.LeftProperty, Mouse.GetPosition(this.GridRoom).X-3);
			Ellipse_StationPosition.SetValue(Canvas.TopProperty, Mouse.GetPosition(this.GridRoom).Y-3);
			if (!Ellipse_StationPosition.IsVisible)
				Ellipse_StationPosition.Visibility = Visibility.Visible;
		}

		private void Button_Cancel(object sender, RoutedEventArgs e)
		{
			if (handler.isBlinking) handler.switchLedBlink(false);
			this.NavigationService.GoBack();
		}

		private void Button_ConfirmNewStation(object sender, RoutedEventArgs e)
		{
			//tryAddStation è già stato chiamato (ha lanciato la GUI)
			if (handler.isBlinking) handler.switchLedBlink(false);
			Station s = ctx.createStation(room, handler.macAddress, meterX, meterY, handler);
			//ctx.saveStation(s); //TODO: de-commentare per salvare su DB
			int i = FindWindowByMAC(handler.macAddress, maxN);
			if (i != maxN)
			{
				Application.Current.Windows[i].Close();
			}
		}
	}
}
