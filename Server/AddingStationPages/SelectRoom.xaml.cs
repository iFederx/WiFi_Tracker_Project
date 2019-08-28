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
	/// Logica di interazione per SelectRoom.xaml
	/// </summary>
	public partial class SelectRoom : Page
	{
		public SelectRoom()
		{
			InitializeComponent();
			//TODO: caricare elenco stanze disponibili
		}

		private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

		}

		private void Button_AddNewRoom(object sender, RoutedEventArgs e)
		{
			//ButtonNewRoom.Content = "Ciaone";
			this.NavigationService.Navigate(new NewRoom());
		}

		private void Button_RoomSelected(object sender, RoutedEventArgs e)
		{
			this.NavigationService.Navigate(new AddToRoom(new Room("prova", 43.2, 53.12))); //TODO: mettere stanza giusta
		}
	}
}
