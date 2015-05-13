using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SensorCoffee.Converter
{
    public class SentimentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int)
            {
                switch ((int)value)
                {
                    case 0:
                        return "NEGATIVE";
                    case 1:
                        return  "POSITIVE";
                    case 2:
                        return "NEUTRAL";
                    default:
                        return "-";
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
