using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BanHangChuNgoc.Helpers
{
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal d)
            {
                return d.ToString("N0") + " k";
            }
            if (value is double db)
            {
                return db.ToString("N0") + " k";
            }
            if (value is int i)
            {
                return i.ToString("N0") + " k";
            }
            return "0 k";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ProfitColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal d)
            {
                return d >= 0 ? new SolidColorBrush(Color.FromRgb(34, 197, 94)) // Green
                              : new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
