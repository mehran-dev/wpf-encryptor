using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace BeRMOoDA.WPF.FileEncryptor
{
    [ValueConversion(typeof(double), typeof(string))]
    public class ProgressBarValueConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string valueText = ((double)value).ToString() + " %";
            return valueText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string valueString = (string)value;
            double valueDouble = double.Parse(valueString.Substring(0, valueString.Length - 2));
            return valueDouble;
        }
    }
}
