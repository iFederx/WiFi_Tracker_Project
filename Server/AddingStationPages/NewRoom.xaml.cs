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
    /// Logica di interazione per NewRoom.xaml
    /// </summary>
    public partial class NewRoom : Page
    {
        public NewRoom()
        {
            InitializeComponent();
        }

		private void Button_Cancel(object sender, RoutedEventArgs e)
		{
			this.NavigationService.GoBack();
		}
	}
}
