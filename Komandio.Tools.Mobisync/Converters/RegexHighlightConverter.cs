using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace Komandio.Tools.Mobisync.Converters;

public class RegexHighlightConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not string valText || values[1] is not string regexPattern)
            return values.Length > 0 ? values[0] : "";

        var text = valText;
        var textBlock = new TextBlock();

        if (string.IsNullOrWhiteSpace(regexPattern))
        {
            textBlock.Inlines.Add(new Run(text));
            return textBlock;
        }

        try
        {
            var lastIndex = 0;
            var matches = Regex.Matches(text, regexPattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                // Add non-matched part
                textBlock.Inlines.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));

                // Add matched part with highlight
                var run = new Run(match.Value)
                {
                    Background = new SolidColorBrush(Color.FromArgb(100, 59, 130, 246)), // blue-500/40
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold
                };
                textBlock.Inlines.Add(run);

                lastIndex = match.Index + match.Length;
            }

            // Add remaining part
            textBlock.Inlines.Add(new Run(text.Substring(lastIndex)));
        }
        catch
        {
            textBlock.Inlines.Clear();
            textBlock.Inlines.Add(new Run(text));
        }

        return textBlock;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}