using SensorCoffee.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SensorCoffee.DataModel
{
    public class MainSampleData : MainViewModel
    {
        public MainSampleData()
        {
            this.Coffee = new Models.TCoffee
            {
                Country = "COSTA RICA",
                CountryKana = "コスタリカ",
                Name1 = "MONTEBRISAS",
                NameKana1 = "モンテブリサス",
                Name2 = "BLACK HONEY",
                NameKana2 = "ブラックハニー",
                Title = "コーヒーの特徴",
                Remarks = "まるで赤ワインのような味わいの中にビターチョコを感じる味わいは冬の寒い夜にオススメです。"
            };
            this.Result = new Models.TResult
            {
                Face = 3,
                Score = 95,
                Sentiment= 1,
                SScore = 95
            };

            this.IsResult = true;
        }
    }
}
