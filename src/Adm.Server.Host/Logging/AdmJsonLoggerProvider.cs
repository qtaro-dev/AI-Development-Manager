using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Adm.Server.Host.Logging;

public sealed class AdmJsonLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly TextWriter writer;
    private readonly object writeGate = new();
    private IExternalScopeProvider scopeProvider = new LoggerExternalScopeProvider();

    public AdmJsonLoggerProvider(TextWriter? writer = null)
    {
        this.writer = writer ?? Console.Error;
    }

    public ILogger CreateLogger(string categoryName) => new Logger(categoryName, this);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        this.scopeProvider = scopeProvider;
    }

    public void Dispose()
    {
        writer.Flush();
    }

    private sealed class Logger(string categoryName, AdmJsonLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            provider.scopeProvider.Push(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            AddStateProperties(properties, state);
            provider.scopeProvider.ForEachScope((scope, target) => AddStateProperties(target, scope), properties);

            var entry = new
            {
                timestamp = DateTimeOffset.UtcNow,
                level = logLevel.ToString(),
                category = categoryName,
                event_id = eventId.Id,
                message = LogRedaction.RedactText(formatter(state, exception)),
                exception_type = exception?.GetType().FullName,
                properties
            };

            lock (provider.writeGate)
            {
                provider.writer.WriteLine(JsonSerializer.Serialize(entry));
                provider.writer.Flush();
            }
        }

        private static void AddStateProperties<TState>(Dictionary<string, object?> properties, TState state)
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                foreach (var pair in pairs)
                {
                    if (pair.Key == "{OriginalFormat}")
                    {
                        continue;
                    }

                    properties[pair.Key] = LogRedaction.RedactValue(pair.Key, pair.Value);
                }
            }
            else if (state is not null)
            {
                properties["scope"] = LogRedaction.RedactText(state.ToString());
            }
        }
    }
}
