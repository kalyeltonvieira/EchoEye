using Microsoft.Extensions.Logging;

namespace EchoEye.Core.Logging;

/// <summary>
/// Custom logger for EchoEye X
/// </summary>
public class EchoEyeLogger : ILogger
{
    private readonly string _categoryName;
    private readonly Action<string, LogLevel, string?> _logAction;

    public EchoEyeLogger(string categoryName, Action<string, LogLevel, string?> logAction)
    {
        _categoryName = categoryName;
        _logAction = logAction;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        if (exception != null)
        {
            message += $" | Exception: {exception.Message}";
        }

        _logAction(_categoryName, logLevel, message);
    }
}

/// <summary>
/// Logger provider for EchoEye X
/// </summary>
public class EchoEyeLoggerProvider : ILoggerProvider
{
    private readonly Action<string, LogLevel, string?> _logAction;
    private readonly Dictionary<string, EchoEyeLogger> _loggers = new();

    public EchoEyeLoggerProvider(Action<string, LogLevel, string?> logAction)
    {
        _logAction = logAction;
    }

    public ILogger CreateLogger(string categoryName)
    {
        if (!_loggers.TryGetValue(categoryName, out var logger))
        {
            logger = new EchoEyeLogger(categoryName, _logAction);
            _loggers[categoryName] = logger;
        }
        return logger;
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}
