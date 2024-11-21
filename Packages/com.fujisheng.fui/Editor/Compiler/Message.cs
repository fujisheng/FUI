using System;
using Unity.Plastic.Newtonsoft.Json;

namespace FUI.Editor
{
    public enum MessageType
    {
        Log = 0,
        Compiler = 1,
    }

    public enum LogLevel
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }

    public class LogMessage
    {
        public LogLevel Level { get; set; }
        public string Message { get; set; }
    }

    public class CompilerMessage
    {
        public string Error { get; set; }
    }

    public class Message
    {
        public MessageType Type { get; set; }
        public string Value { get; set; }

        public static void WriteMessage<T>(MessageType type, T message) where T : class
        {
            if (message == null)
            {
                return;
            }

            var msgString = JsonConvert.SerializeObject(message);
            var msg = new Message
            {
                Type = type,
                Value = msgString
            };
            Console.WriteLine(JsonConvert.SerializeObject(msg));
        }

        public static object ReadMessage(string msgString)
        {
            if (string.IsNullOrEmpty(msgString))
            {
                return null;
            }

            var msg = JsonConvert.DeserializeObject<Message>(msgString);
            switch (msg.Type)
            {
                case MessageType.Log:
                    return JsonConvert.DeserializeObject<LogMessage>(msg.Value);
                case MessageType.Compiler:
                    return JsonConvert.DeserializeObject<CompilerMessage>(msg.Value);
                default:
                    return null;
            }
        }
    }
}
