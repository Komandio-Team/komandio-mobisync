namespace Komandio.Tools.Mobisync.Messages;

public class ScrollToFirstMatchMessage
{
    public ScrollToFirstMatchMessage(string regexPattern)
    {
        RegexPattern = regexPattern;
    }

    public string RegexPattern { get; }
}