namespace Komandio.Tools.Mobisync.Messages
{
    public class ApplicationStateMessage
    {
        public bool IsRunning { get; }
        public bool IsSimulating { get; }

        public ApplicationStateMessage(bool isRunning, bool isSimulating)
        {
            IsRunning = isRunning;
            IsSimulating = isSimulating;
        }
    }
}
