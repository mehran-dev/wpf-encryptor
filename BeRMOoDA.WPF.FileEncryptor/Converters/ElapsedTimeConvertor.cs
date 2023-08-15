using System;
using System.Windows.Data;

namespace BeRMOoDA.WPF.FileEncryptor
{
    [ValueConversion(typeof(int), typeof(string))]
    public class ElapsedTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TimeSpan t = TimeSpan.FromSeconds((int)value);
            return t.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TimeSpan t = TimeSpan.Parse((string)value);
            return t.TotalSeconds;
        }
    }
}
