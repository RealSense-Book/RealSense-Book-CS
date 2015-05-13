using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SensorCoffee.Converter
{
    public class FaceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int)
            {
                switch ((int)value)
                {
                    case 0:
                        return "ANGER";
                    case 1:
                        return "CONTEMPT";
                    case 2:
                        return "DISGUST";
                    case 3:
                        return "FEAR";
                    case 4:
                        return "JOY";
                    case 5:
                        return "SADNESS";
                    case 6:
                        return "SURPRISE";
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
