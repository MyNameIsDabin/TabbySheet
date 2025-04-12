using System;

namespace TabbySheet
{
    public static class Logger
    {
        public enum LogType
        {
            Info,
            Debug
        }
        
        private static Action<LogType, string> LogAction { get; set; }

        public static void SetLogAction(Action<LogType, string> logAction) => Logger.LogAction = logAction;

        public static void Log(string message, LogType logType = LogType.Info)
        {
            if (LogAction == null)
                Console.WriteLine(message);
            
            LogAction?.Invoke(logType, message);
        }
    }
}