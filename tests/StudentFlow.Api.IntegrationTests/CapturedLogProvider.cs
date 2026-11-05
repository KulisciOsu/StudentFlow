using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace StudentFlow.Api.IntegrationTests;

/// <summary>Zachytává zprávy i připojené výjimky pro testy úniku citlivých údajů do logů.</summary>
internal sealed class CapturedLogProvider(ConcurrentQueue<string> messages) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new CapturedLogger(messages);
    public void Dispose() { }

    private sealed class CapturedLogger(ConcurrentQueue<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => messages.Enqueue(formatter(state, exception) + (exception is null ? "" : "\n" + exception));
    }
}
