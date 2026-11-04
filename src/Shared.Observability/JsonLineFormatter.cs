using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Shared.Observability;

public sealed class JsonLineFormatter() : ConsoleFormatter(FormatterName)
{
    public const string FormatterName = "jsonline";

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        ArgumentNullException.ThrowIfNull(textWriter);

        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            json.WriteStartObject();
            json.WriteString("time", DateTimeOffset.UtcNow);
            json.WriteString("level", LevelName(logEntry.LogLevel));
            json.WriteString("category", logEntry.Category);
            json.WriteString("message", message);

            if (logEntry.State is IReadOnlyList<KeyValuePair<string, object?>> fields)
            {
                foreach (var (key, value) in fields)
                {
                    if (key != "{OriginalFormat}")
                    {
                        WriteField(json, JsonNamingPolicy.SnakeCaseLower.ConvertName(key), value);
                    }
                }
            }

            if (logEntry.Exception is not null)
            {
                json.WriteString("exception", logEntry.Exception.ToString());
            }

            json.WriteEndObject();
        }

        textWriter.WriteLine(Encoding.UTF8.GetString(buffer.ToArray()));
    }

    private static void WriteField(Utf8JsonWriter json, string name, object? value)
    {
        switch (value)
        {
            case null:
                json.WriteNull(name);
                break;
            case int number:
                json.WriteNumber(name, number);
                break;
            case long number:
                json.WriteNumber(name, number);
                break;
            case double number:
                json.WriteNumber(name, number);
                break;
            case bool flag:
                json.WriteBoolean(name, flag);
                break;
            default:
                json.WriteString(name, value.ToString());
                break;
        }
    }

    private static string LevelName(LogLevel level) => level switch
    {
        LogLevel.Trace => "trace",
        LogLevel.Debug => "debug",
        LogLevel.Information => "info",
        LogLevel.Warning => "warn",
        LogLevel.Error => "error",
        LogLevel.Critical => "critical",
        _ => "none",
    };
}
