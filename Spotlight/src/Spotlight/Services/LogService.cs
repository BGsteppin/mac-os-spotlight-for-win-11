using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace WinSpotlight.Services
{
    public static class LogService
    {
        public static ILogger Logger { get; private set; } = null!;
        private static StreamWriter? _writer;
        private static readonly object _lock = new();

        public static void Initialize()
        {
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinSpotlight", "logs");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "spotlight.log");
            Rotate(logPath);
            _writer = new StreamWriter(File.Open(logPath, FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = true };

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddProvider(new TextWriterLoggerProvider(_writer));
            });
            Logger = loggerFactory.CreateLogger("WinSpotlight");
        }

        private static void Rotate(string path)
        {
            if (File.Exists(path) && new FileInfo(path).Length > 1_000_000)
            {
                var archive = path + ".1";
                File.Delete(archive);
                File.Move(path, archive);
            }
        }
    }

    internal sealed class TextWriterLoggerProvider : ILoggerProvider
    {
        private readonly TextWriter _writer;

        public TextWriterLoggerProvider(TextWriter writer)
        {
            _writer = writer;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TextWriterLogger(_writer);
        }

        public void Dispose()
        {
        }
    }

    internal sealed class TextWriterLogger : ILogger
    {
        private readonly TextWriter _writer;
        private static readonly object _lock = new();

        public TextWriterLogger(TextWriter writer)
        {
            _writer = writer;
        }

        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (_lock)
            {
                _writer.WriteLine($"{DateTime.Now:O} [{logLevel}] {formatter(state, exception)} {exception}");
            }
        }
    }
}
