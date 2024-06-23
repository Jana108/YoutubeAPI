using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoutubeAPI.Utils
{
    public static class LoggerAsync
    {
        private static readonly BlockingCollection<LogMessage> _logQueue;
        private static readonly CancellationTokenSource _cancellationTokenSource;
        private static readonly Task _logTask;

        static LoggerAsync()
        {
            _logQueue = [];
            _cancellationTokenSource = new();
            _logTask = Task.Factory.StartNew(ProcessLogQueue, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public static void Log(LogLevel level, string message)
        {
            var logMessage = new LogMessage
            {
                Level = level,
                Message = message,
                Timestamp = DateTime.Now
            };
            _logQueue.Add(logMessage);
        }

        private static void ProcessLogQueue()
        {
            foreach (var logMessage in _logQueue.GetConsumingEnumerable())
            {
                var color = GetColor(logMessage.Level);
                Console.ForegroundColor = color;
                Console.WriteLine($"[{logMessage.Timestamp:HH:mm:ss} {logMessage.Level}] {logMessage.Message}");
                Console.ResetColor();
            }
        }

        private static ConsoleColor GetColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => ConsoleColor.Blue,
                LogLevel.Info => ConsoleColor.Green,
                LogLevel.Warn => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.FatalError => ConsoleColor.DarkRed,
                _ => ConsoleColor.White,
            };
        }

        public static void Dispose()
        {
            _logQueue.CompleteAdding();
            _cancellationTokenSource.Cancel();
            _logTask.Wait();
            _logQueue.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }
}
