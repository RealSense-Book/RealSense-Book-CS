using System.Windows.Controls;
using System.Windows.Input;

namespace SensorCoffee.Views
{
    /// <summary>
    /// CoffeePage.xaml の相互作用ロジック
    /// </summary>
    public partial class CoffeePage : Page
    {
        public CoffeePage()
        {
            InitializeComponent();
            this.DataContext = App.MainVM;
        }

        private void Cup_Clicked(object sender, MouseButtonEventArgs e)
        {
            this.NavigationService.Navigate(new LaunchPage());
        }

        private void Cup_Tapped(object sender, TouchEventArgs e)
        {
            this.NavigationService.Navigate(new LaunchPage());
        }
    }
}
