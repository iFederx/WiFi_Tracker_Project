﻿using System;
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
	/// Logica di interazione per SelectRoom.xaml
	/// </summary>
	public partial class SelectRoom : Page
	{
		Context ctx;
		Brush defaultColor;
		int selected = 0;
		StationHandler handler = null;

		public SelectRoom()
		{
			InitializeComponent();
		}

		internal SelectRoom(Context _ctx, StationHandler _handler)
		{
			InitializeComponent();
			ctx = _ctx;
			handler = _handler;
			defaultColor = ((Button)RoomsContainer.Children[1]).Background;
			RoomsContainer.Children.RemoveRange(0, RoomsContainer.Children.Count);
			foreach (Room r in ctx.getRooms())
			{
				Button b = new Button();
				b.Content = r.roomName;
				Thickness margin = b.Margin;
				margin.Bottom = 5;
				b.Margin = margin;
				if (b.Content.ToString() == "External")
					b.Visibility = Visibility.Collapsed; //non mostro la stanza External
				b.Click += new RoutedEventHandler(Button_RoomSelected);
				RoomsContainer.Children.Add(b);
			}
		}

		private void Button_RoomSelected(object sender, RoutedEventArgs e)
		{
			int i = -1;
			foreach (Button b in RoomsContainer.Children)
			{
				b.Background = defaultColor;
				i++;
				if (((Button)sender).Equals(b))
					selected = i;
			}
				
			((Button)sender).Background = Brushes.DarkGray;
			But_Continue.IsEnabled = true;
		}

		private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

		}

		private void Button_AddNewRoom(object sender, RoutedEventArgs e)
		{
			this.NavigationService.Navigate(new NewRoom(ctx, handler));
		}

		private void Button_Continue(object sender, RoutedEventArgs e)
		{
			Room r = ctx.getRoom(((Button)RoomsContainer.Children[selected]).Content.ToString());
			this.NavigationService.Navigate(new AddToRoom(ctx, r, handler));
		}

		private void Image_RechargeRooms(object sender, MouseButtonEventArgs e)
		{
			//TODO: implementare ricarica stanze
		}
	}
}
