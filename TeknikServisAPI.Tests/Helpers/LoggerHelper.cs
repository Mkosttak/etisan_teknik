using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TeknikServisAPI.Tests.Helpers
{
    /// <summary>
    /// Helper for creating mock/fake logger instances for testing
    /// Provides both NullLogger (for silent testing) and TestLogger (for verification)
    /// 
    /// Usage:
    ///   var logger = LoggerHelper.CreateNullLogger<MyService>();
    ///   var service = new MyService(logger);
    /// </summary>
    public static class LoggerHelper
    {
        /// <summary>
        /// Creates a NullLogger that doesn't log anything
        /// Use for tests where logging is not being verified
        /// </summary>
        public static ILogger<T> CreateNullLogger<T>()
        {
            return NullLogger<T>.Instance;
        }
        
        /// <summary>
        /// Creates a TestLogger that captures log messages
        /// Use when you need to verify logging behavior
        /// </summary>
        public static TestLogger<T> CreateTestLogger<T>()
        {
            return new TestLogger<T>();
        }
    }
    
    /// <summary>
    /// Test logger that captures log entries for verification
    /// Implements ILogger for dependency injection
    /// </summary>
    public class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> LogEntries { get; } = new();
        
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }
        
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }
        
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            LogEntries.Add(new LogEntry
            {
                LogLevel = logLevel,
                EventId = eventId,
                Message = formatter(state, exception),
                Exception = exception
            });
        }
        
        /// <summary>
        /// Helper to check if a specific message was logged
        /// </summary>
        public bool HasLoggedMessage(string message, LogLevel? level = null)
        {
            return LogEntries.Any(e => 
                e.Message.Contains(message) && 
                (level == null || e.LogLevel == level));
        }
        
        /// <summary>
        /// Helper to get all error logs
        /// </summary>
        public IEnumerable<LogEntry> GetErrorLogs()
        {
            return LogEntries.Where(e => e.LogLevel == LogLevel.Error);
        }
    }
    
    /// <summary>
    /// Represents a captured log entry
    /// </summary>
    public class LogEntry
    {
        public LogLevel LogLevel { get; set; }
        public EventId EventId { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }
}
