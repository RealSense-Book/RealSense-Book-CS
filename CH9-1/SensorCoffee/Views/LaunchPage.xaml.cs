using System.Windows;
using System.Windows.Controls;

namespace SensorCoffee.Views
{
    /// <summary>
    /// LaunchPage.xaml の相互作用ロジック
    /// </summary>
    public partial class LaunchPage : Page
    {
        public LaunchPage()
        {
            InitializeComponent();
            this.DataContext = App.MainVM;
            while (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.RemoveBackEntry();
            }
        }

        private void TryButton_Click(object sender, RoutedEventArgs e)
        {
            App.MainVM.Connect();
            this.NavigationService.Navigate(new CameraPage());
        }
    }
}
