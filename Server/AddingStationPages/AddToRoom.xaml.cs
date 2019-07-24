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

namespace Server.AddingStationPages
{
    /// <summary>
    /// Logica di interazione per AddToRoom.xaml
    /// </summary>
    public partial class AddToRoom : Page
    {
		private double roomX;
		private double roomY;
		private double aspectRatio = 1.77777;

		public AddToRoom()
        {
            InitializeComponent();
        }

		private void Grid_MouseMove(object sender, MouseEventArgs e)
		{
			//TODO_FEDE: recuperare misure stanza
			roomX = 10;
			roomY = 5;
			aspectRatio = roomX / roomY;

			double meterX = Mouse.GetPosition(this.GridRoom).X / this.GridRoom.Width * roomX;
			double meterY = Mouse.GetPosition(this.GridRoom).Y / this.GridRoom.Height * roomY;

			xLabel.Content = meterX.ToString("X: 0.## m");
			yLabel.Content = meterY.ToString("Y: 0.## m");
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			//TODO_FEDE: da perfezionare
			if (sizeInfo.NewSize.Width < sizeInfo.NewSize.Height + 150)
			{
				AdjustSizeByWidth(sizeInfo);
			}
			else
			{
				AdjustSizeByHeight(sizeInfo);
			}
		}

		private void AdjustSizeByHeight(SizeChangedInfo sx)
		{
			this.GridRoom.Height = sx.NewSize.Height - 100;
			this.GridRoom.Width = this.GridRoom.Height * aspectRatio;
		}

		private void AdjustSizeByWidth(SizeChangedInfo sx)
		{
			this.GridRoom.Width = sx.NewSize.Width - 100;
			this.GridRoom.Height = this.GridRoom.Width / aspectRatio;
		}

		private void Grid_NewPosition(object sender, MouseButtonEventArgs e)
		{
			double meterX = Mouse.GetPosition(this.GridRoom).X / this.GridRoom.Width * roomX;
			double meterY = Mouse.GetPosition(this.GridRoom).Y / this.GridRoom.Height * roomY;
			LastX.Content = meterX.ToString("Last X: 0.## m");
			LastY.Content = meterY.ToString("Last Y: 0.## m");
			Button_ConfirmPosition.IsEnabled = true;
			Ellipse_StationPosition.SetValue(Canvas.LeftProperty, Mouse.GetPosition(this.GridRoom).X);
			Ellipse_StationPosition.SetValue(Canvas.TopProperty, Mouse.GetPosition(this.GridRoom).Y);
			if (!Ellipse_StationPosition.IsVisible)
				Ellipse_StationPosition.Visibility = Visibility.Visible;
		}
	}
}
