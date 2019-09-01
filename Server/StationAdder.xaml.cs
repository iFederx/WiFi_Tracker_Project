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
using System.Windows.Shapes;

namespace Panopticon
{
    /// <summary>
    /// Logica di interazione per Window1.xaml
    /// </summary>
    public partial class StationAdder : Window
    {
		public StationAdder()
        {
            InitializeComponent();
			FrameNewStation.Content = new AddingStationPages.SelectRoom();
        }

		internal StationAdder(Context _ctx, StationHandler _handler)
		{
			InitializeComponent();
			this.Title += " (" + _handler.macAddress + ")";
			FrameNewStation.Content = new AddingStationPages.SelectRoom(_ctx, _handler);
		}
	}
}
