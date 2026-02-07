namespace Komandio.Tools.Mobisync.Messages
{
    public class LogPathChangedMessage
    {
        public string NewPath { get; }
        public LogPathChangedMessage(string newPath) => NewPath = newPath;
    }
}
