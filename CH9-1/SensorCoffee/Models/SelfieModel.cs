using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SensorCoffee.Models
{

    public class TCoffee
    {
        public string Country { get; set; }
        public string CountryKana { get; set; }
        public string Name1 { get; set; }
        public string NameKana1 { get; set; }
        public string Name2 { get; set; }
        public string NameKana2 { get; set; }
        public string Title { get; set; }
        public string Remarks { get; set; }
    }

    public class SelfieModel : INotifyPropertyChanged
    {
        private const int EX_ANGER = 0;
        private const int EX_CONTEMPT = 1;
        private const int EX_DISGUST = 2;
        private const int EX_NEUTRAL = 3;
        private const int EX_HAPPINESS = 4;
        private const int EX_SADNESS = 5;
        private const int EX_SURPRISE = 6;

        private const int COFFEE_INDONESIA = 0;
        private const int COFFEE_BRAZIL = 1;
        private const int COFFEE_COSTA = 2;
        private const int COFFEE_ETHIOPIA = 3;
        
        private Dictionary<int, Dictionary<string, int>> Matrix4 = new Dictionary<int, Dictionary<string, int>>() {
            { EX_NEUTRAL, new Dictionary<string, int> () { { "A", COFFEE_BRAZIL }, { "B", COFFEE_COSTA } } },
            { EX_HAPPINESS, new Dictionary<string, int> () { { "A", COFFEE_ETHIOPIA }, { "B", COFFEE_BRAZIL } } },
            { EX_SURPRISE, new Dictionary<string, int> () { { "A", COFFEE_ETHIOPIA }, { "B", COFFEE_BRAZIL } } },
            { EX_ANGER, new Dictionary<string, int> () { { "A", COFFEE_BRAZIL }, { "B", COFFEE_INDONESIA } } },
            { EX_SADNESS, new Dictionary<string, int> () { { "A", COFFEE_COSTA }, { "B", COFFEE_ETHIOPIA } } }
        };

        private TResult _Result = null;
        public TResult Result
        {
            get { return this._Result; }
            set
            {
                this._Result = value;
                OnPropertyChanged();
            }
        }

        private TCoffee _Coffee = null;
        public TCoffee Coffee
        {
            get { return this._Coffee; }
            set
            {
                this._Coffee = value;
                OnPropertyChanged();
            }
        }

        private int _Counter = 3;
        public int Counter {
            get { return this._Counter; }
            set {
                this._Counter = value;
                OnPropertyChanged ();
            }
        }

        private ObservableCollection<TCoffee> Coffees = new ObservableCollection<TCoffee>();

        public SelfieModel()
        {
            GetSettings();
        }

        public async Task InitializePreview()
        {
            this.Counter = 3;
            await Task.Delay(1);
        }

        public async Task StartPreview()
        {
            try
            {
                this.Counter = 3;
                await System.Threading.Tasks.Task.Delay(1000);
                this.Counter = 2;
                await System.Threading.Tasks.Task.Delay(1000);
                this.Counter = 1;
                await System.Threading.Tasks.Task.Delay(1000);
                this.Counter = 0;
            }
            catch
            {
            }
        }

        public async Task StopPreview ()
        {
            await Task.Delay (1);
            SetCoffee(this.Result.Face, this.Result.Score);
            OnPropertyChanged ("Result");
        }

        private void SetCoffee(int face, int score)
        {
            var resultPoint = score;
            int value = 0;

            try
            {
                if (resultPoint >= 60)
                {
                    value = Matrix4[face]["A"];
                }
                else
                {
                    value = Matrix4[face]["B"];
                }
            }
            catch
            {
                /* 範囲外の場合はナチュラル相当 */
                if (resultPoint >= 60)
                {
                    value = Matrix4[EX_NEUTRAL]["A"];
                }
                else
                {
                    value = Matrix4[EX_NEUTRAL]["B"];
                }
            }
            this.Coffee = this.Coffees[value];
        }

        private void GetSettings()
        {
            this.Coffees.Add(new TCoffee
            {
                Country = "INDONESIA",
                CountryKana = "インドネシア",
                Name1 = "ALUR BADAK",
                NameKana1 = "アルールバダ",
                Title = "コーヒーの特徴",
                Remarks = "しっかりとした質感とカカオなどのナッツテイストが疲れた時にピッタリです。"
            });
            this.Coffees.Add(new TCoffee
            {
                Country = "BRAZIL",
                CountryKana = "ブラジル",
                Name1 = "OURO VERDE",
                NameKana1 = "オーロヴェルジ農園",
                Title = "コーヒーの特徴",
                Remarks = "レッドアップルの甘酸っぱさとキャラメルの風味がすっきり気分をリフレッシュさせてくれます。"
            });
            this.Coffees.Add(new TCoffee
            {
                Country = "COSTA RICA",
                CountryKana = "コスタリカ",
                Name1 = "MONTEBRISAS",
                NameKana1 = "モンテブリサス",
                Name2 = "BLACK HONEY",
                NameKana2 = "ブラックハニー",
                Title = "コーヒーの特徴",
                Remarks = "まるで赤ワインのような味わいの中にビターチョコを感じる味わいは冬の寒い夜にオススメです。"
            });
            this.Coffees.Add(new TCoffee
            {
                Country = "ETHIOPIA",
                CountryKana = "エチオピア",
                Name1 = "YIRGACHEFFE",
                NameKana1 = "イルガチェフェ",
                Name2 = "KOCHERE",
                NameKana2 = "コチャレ",
                Title = "コーヒーの特徴",
                Remarks = "豊かなベリーの風味とフルーティーな味わいは朝や休憩時間を優雅に過ごすお供にどうぞ。"
            });
        }

        public delegate void FaildHandler(object sender, string e);
        public event FaildHandler Faild;
        protected virtual void OnFaild(string line)
        {
            if (Faild != null)
                Faild(this, line);
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
