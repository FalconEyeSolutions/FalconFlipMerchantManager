using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace FalconFlipMerchantManager
{
    public class CustomConsoleFormatter : ConsoleFormatter
    {
        public CustomConsoleFormatter() : base("custom") { }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
        {
            var originalColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = GetConsoleColor(logEntry.LogLevel);

                var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
                if (!string.IsNullOrEmpty(message))
                {
                    Console.WriteLine(message);
                }
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }

        private ConsoleColor GetConsoleColor(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Critical => ConsoleColor.Red,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Information => ConsoleColor.White,
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Trace => ConsoleColor.DarkGray,
                _ => Console.ForegroundColor
            };
        }
    }
}