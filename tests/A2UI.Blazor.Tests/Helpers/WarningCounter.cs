using Microsoft.Extensions.Logging;

namespace A2UI.Blazor.Tests.Helpers;

/// <summary>
/// Minimal ILogger that counts warning-level log calls.
/// </summary>
internal sealed class WarningCounter<T> : ILogger<T>
{
    public int Count { get; private set; }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.Warning) Count++;
    }
}
