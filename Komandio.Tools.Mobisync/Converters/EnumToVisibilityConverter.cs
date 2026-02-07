using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Komandio.Tools.Mobisync.Converters;

public class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return Visibility.Collapsed;

        var currentState = value.ToString() ?? "";
        var targetState = parameter.ToString() ?? "";

        var match = string.Equals(currentState, targetState, StringComparison.OrdinalIgnoreCase);
        return match ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}