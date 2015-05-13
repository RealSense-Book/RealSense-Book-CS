using System.Windows;
using System.Windows.Controls;

namespace SensorCoffee.Views
{
    /// <summary>
    /// ResultPage.xaml の相互作用ロジック
    /// </summary>
    public partial class ResultPage : Page
    {
        public ResultPage()
        {
            InitializeComponent();
            this.DataContext = App.MainVM;
        }

        private void Recommend_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new CoffeePage());
        }
    }
}
