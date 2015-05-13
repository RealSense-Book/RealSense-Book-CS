using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SensorCoffee.Converter
{
    public class ScoreTextKanaConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int)
            {
                var score = (int)value ;
                if (score >= 90)
                {
                    return "いい表情ですね！";
                }
                else if (score >= 20)
                {
                    return "まあまあの表情！";
                }
                else
                {
                    return "もっと表情を出して！";
                } 
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
