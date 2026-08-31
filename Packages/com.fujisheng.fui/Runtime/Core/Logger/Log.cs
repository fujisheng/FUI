namespace FUI.Diagnostics
{
    /// <summary>
    /// 抽象框架日志的普通、警告、错误和异常输出目标。
    /// </summary>
    public interface ILogSink
    {
        void Log(object message);
        void LogWarning(object message);
        void LogError(object message);

        void LogException(System.Exception exception);
    }

    /// <summary>
    /// 为框架消息统一添加 FUI 标签，并转发到应用配置的日志输出目标。
    /// </summary>
    public class Log
    {
        const string TAG = "[FUI]:";

        ILogSink logger;

        static readonly Log instance = new Log();

        public static Log Instance => instance;

        public void SetLogger(ILogSink customLogger)
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

        public void Write(object message)
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
