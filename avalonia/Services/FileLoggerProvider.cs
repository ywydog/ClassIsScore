using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

/// <summary>
/// 简单的文件日志提供者
/// </summary>
public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logFilePath;
    private readonly LogLevel _minimumLevel;
    private readonly object _lock = new();

    public FileLoggerProvider(string logFilePath, LogLevel minimumLevel = LogLevel.Debug)
    {
        _logFilePath = logFilePath;
        _minimumLevel = minimumLevel;

        // 确保日志目录存在
        var dir = Path.GetDirectoryName(logFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(this, categoryName);
    }

    public void Dispose() { }

    internal void WriteLog(string message)
    {
        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logFilePath, message);
            }
            catch
            {
                // 日志写入失败，忽略
            }
        }
    }

    internal bool IsEnabled(LogLevel logLevel) => logLevel >= _minimumLevel;

    private class FileLogger : ILogger
    {
        private readonly FileLoggerProvider _provider;
        private readonly string _categoryName;

        public FileLogger(FileLoggerProvider provider, string categoryName)
        {
            _provider = provider;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => _provider.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var level = logLevel.ToString().ToUpper().PadLeft(7);
            var message = formatter(state, exception);
            var line = $"[{timestamp}] [{level}] [{_categoryName}] {message}";

            if (exception != null)
            {
                line += $"{Environment.NewLine}{exception}";
            }

            line += Environment.NewLine;

            _provider.WriteLog(line);
        }
    }
}

/// <summary>
/// FileLogger 扩展方法
/// </summary>
public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string logFilePath, LogLevel minimumLevel = LogLevel.Debug)
    {
        builder.AddProvider(new FileLoggerProvider(logFilePath, minimumLevel));
        return builder;
    }
}
