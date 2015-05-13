using SensorCoffee.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SensorCoffee.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private SynchronizationContext Context = SynchronizationContext.Current;
        private Models.SelfieModel Model = new Models.SelfieModel();
        private Models.RSModel RS = new Models.RSModel();

        public MainViewModel()
        {
            this.RS.PropertyChanged += RS_PropertyChanged;
            this.Model.PropertyChanged += Model_PropertyChanged;
        }

        public ImageSource ColorImageElement
        {
            get { return this.RS.ColorImageElement; }
            set { this.RS.ColorImageElement = value; }
        }

        private ImageSource _TempImageElement;
        public ImageSource TempImageElement
        {
            get { return this._TempImageElement; }
            set { 
                this._TempImageElement = value;
                OnPropertyChanged();
            }
        }

        public string Message
        {
            get { return this.RS.Message; }
            set { this.RS.Message = value; }
        }

        public bool IsResult
        {
            get { return this.RS.IsResult; }
            set { this.RS.IsResult = value; }
        }

        public TResult Result
        {
            get { return this.Model.Result; }
            set { this.Model.Result = value; }
        }

        public TCoffee Coffee
        {
            get { return this.Model.Coffee; }
            set { this.Model.Coffee = value; }
        }

        public int Counter
        {
            get { return this.Model.Counter; }
            set
            {
                this.Model.Counter  = value;
                OnPropertyChanged();
            }
        }
        public void Connect()
        {
            this.RS.RSStart();
        }

        public void DisConnect()
        {
            this.RS.RSStop();
        }

        public async Task StartPreview()
        {
            await this.Model.StartPreview();
        }

        public async Task StopPreview()
        {
            await this.Model.StopPreview();
        }

        private void RS_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.Context.Post((o) =>
            {
                OnPropertyChanged(e.PropertyName);
            }, null);
            if (e.PropertyName == "Result" && this.RS.Result != null)
            {
                if (this.Counter > 1)
                {
                    this.Model.Result = this.RS.Result;
                }
            }
        }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.Context.Post((o) =>
                {
                    OnPropertyChanged(e.PropertyName);
                }, null);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var eventHandler = this.PropertyChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
