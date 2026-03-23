namespace FUI
{
    public interface ILogger
    {
        void Log(object message);
        void LogWarning(object message);
        void LogError(object message);

        void LogException(System.Exception exception);
    }

    public class Logger
    {
        const string TAG = "[FUI]:";

        ILogger logger;

        static readonly Logger instance = new Logger();

        public static Logger Instance => instance;

        public void SetLogger(ILogger customLogger)
        {
            logger = customLogger;
        }

        public void Enable(bool enable)
        {
            if (!enable)
            {
                logger = null;
            }
        }

        public void Log(object message)
        {
            logger?.Log($"{TAG} {message}");
        }

        public void LogWarning(object message)
        {
            logger?.LogWarning($"{TAG} {message}");
        }

        public void LogError(object message)
        {
            logger?.LogError($"{TAG} {message}");
        }

        public void LogException(System.Exception exception)
        {
            logger?.LogException(exception);
        }
    }
}