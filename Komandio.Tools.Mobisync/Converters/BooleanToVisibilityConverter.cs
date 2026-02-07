using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Komandio.Tools.Mobisync.Converters;

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var flag = false;
        if (value is bool b) flag = b;

        if (parameter is string s && s == "Inverse") flag = !flag;

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}