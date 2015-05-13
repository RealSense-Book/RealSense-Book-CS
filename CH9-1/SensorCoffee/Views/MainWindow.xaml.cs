using System.Windows.Navigation;

namespace SensorCoffee.Views
{
    public partial class MainWindow : NavigationWindow 
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = App.MainVM;
            this.NavigationService.Navigate(new LaunchPage());
        }
    }
}
