using System.Windows.Controls;

namespace SensorCoffee.Views
{
    public partial class CameraPage : Page
    {
        private bool isResult;
        
        public CameraPage()
        {
            InitializeComponent();
            this.DataContext = App.MainVM;
            App.MainVM.PropertyChanged += MainVM_PropertyChanged;
        }

        private void MainVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Result")
            {
                if (!this.isResult)
                {
                    if (App.MainVM.Result != null)
                    {
                        this.isResult = true;
                        CountDown();
                    }
                }
            }
        }

        private async void CountDown()
        {
            await App.MainVM.StartPreview();
            App.MainVM.TempImageElement = App.MainVM.ColorImageElement;
            App.MainVM.DisConnect();
            await App.MainVM.StopPreview();
            this.NavigationService.Navigate(new ResultPage());
        }
    }
}
