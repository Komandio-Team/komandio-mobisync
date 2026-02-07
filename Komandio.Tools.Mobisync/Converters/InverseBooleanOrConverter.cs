using System.Globalization;
using System.Windows.Data;

namespace Komandio.Tools.Mobisync.Converters;

public class InverseBooleanOrConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length == 0) return true;
        var isAnyRunning = values.OfType<bool>().Any(b => b);
        return !isAnyRunning;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}