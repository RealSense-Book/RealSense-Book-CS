using System.Windows;

namespace SensorCoffee
{
    public partial class App : Application
    {
        private static SensorCoffee.ViewModels.MainViewModel _MainVM = null;
        public static SensorCoffee.ViewModels.MainViewModel MainVM
        {
            get
            {
                if (_MainVM == null)
                {
                    _MainVM = new SensorCoffee.ViewModels.MainViewModel();
                }
                return _MainVM;
            }
        }
    }
}
