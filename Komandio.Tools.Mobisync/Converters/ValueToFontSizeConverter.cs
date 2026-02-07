using System.Globalization;
using System.Windows.Data;

namespace Komandio.Tools.Mobisync.Converters;

public class ValueToFontSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string text || string.IsNullOrEmpty(text))
            return 16.0;

        // Base font size reduced from 18 to 16
        // If text is long, shrink it further
        if (text.Length > 25) return 10.0;
        if (text.Length > 15) return 12.0;
        
        return 16.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
