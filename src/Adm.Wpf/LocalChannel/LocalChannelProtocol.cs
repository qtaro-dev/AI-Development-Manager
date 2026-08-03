using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using Adm.Wpf.Shell;

namespace Adm.Wpf.LocalChannel;

public static class LocalChannelProtocol
{
    public const int Version = 1;
    public const int MaxMessageBytes = 1024 * 1024;
    public const int MaxJsonDepth = 16;
    public const int MaxRequestIdLength = 64;
    public const int MaxOperationLength = 100;
    private static readonly Regex SafeRequestId = new("^[A-Za-z0-9_-]{1,64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex SafeOperation = new("^[A-Za-z][A-Za-z0-9]*(?:[._-][A-Za-z0-9]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static LocalChannelRequest ParseRequest(string json, string? source)
    {
        EnsureMessageSize(json);
        EnsureTopLevelSource(source);

        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = MaxJsonDepth, CommentHandling = JsonCommentHandling.Disallow });
            var root = document.RootElement;
            RequireObject(root);
            RequireExactProperties(root, "version", "kind", "requestId", "operation", "payload");

            var version = ReadVersion(root);
            if (version != Version)
                throw new LocalChannelProtocolException("unsupported_version", "errors.localChannel.unsupportedVersion", null);

            var kind = ReadString(root, "kind", "invalid_request", "errors.localChannel.invalidRequest");
            if (!string.Equals(kind, "request", StringComparison.Ordinal))
                throw new LocalChannelProtocolException("invalid_request", "errors.localChannel.invalidRequest", ReadOptionalRequestId(root));

            var requestId = ReadRequestId(root);
            var operation = ReadOperation(root, requestId);
            var payload = root.GetProperty("payload");
            if (payload.ValueKind is not (JsonValueKind.Object or JsonValueKind.Null))
                throw new LocalChannelProtocolException("invalid_request", "errors.localChannel.invalidRequest", requestId);

            return new LocalChannelRequest(requestId, operation, payload.Clone());
        }
        catch (LocalChannelProtocolException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw new LocalChannelProtocolException("invalid_json", "errors.localChannel.invalidJson", null);
        }
    }

    public static ILocalChannelMessage ParseMessage(string json)
    {
        EnsureMessageSize(json);

        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = MaxJsonDepth, CommentHandling = JsonCommentHandling.Disallow });
            var root = document.RootElement;
            RequireObject(root);
            var kind = ReadString(root, "kind", "invalid_request", "errors.localChannel.invalidRequest");
            var version = ReadVersion(root);
            if (version != Version)
                throw new LocalChannelProtocolException("unsupported_version", "errors.localChannel.unsupportedVersion", ReadOptionalRequestId(root));
            var requestId = ReadRequestId(root);

            if (string.Equals(kind, "response", StringComparison.Ordinal))
            {
                RequireExactProperties(root, "version", "kind", "requestId", "result");
                var result = root.GetProperty("result");
                if (result.ValueKind is not (JsonValueKind.Object or JsonValueKind.Null))
                    throw new LocalChannelProtocolException("invalid_request", "errors.localChannel.invalidRequest", requestId);
                return new LocalChannelResponse(requestId, result.Clone());
            }

            if (string.Equals(kind, "error", StringComparison.Ordinal))
            {
                RequireExactProperties(root, "version", "kind", "requestId", "error");
                var error = root.GetProperty("error");
                RequireObject(error);
                RequireExactProperties(error, "code", "messageKey");
                var code = ReadString(error, "code", "invalid_request", "errors.localChannel.invalidRequest");
                var messageKey = ReadString(error, "messageKey", "invalid_request", "errors.localChannel.invalidRequest");
                return new LocalChannelError(requestId, code, messageKey);
            }

            throw new LocalChannelProtocolException("invalid_request", "errors.localChannel.invalidRequest", requestId);
        }
        catch (LocalChannelProtocolException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw new LocalChannelProtocolException("invalid_json", "errors.localChannel.invalidJson", null);
        }
    }

    public static string SerializeResponse(LocalChannelResponse response)
    {
        EnsureRequestId(response.RequestId);
        return JsonSerializer.Serialize(new
        {
            version = Version,
            kind = "response",
            requestId = response.RequestId,
            result = response.Result,
        }, WebJsonOptions);
    }

    public static string SerializeError(LocalChannelError error)
    {
        var requestId = string.IsNullOrWhiteSpace(error.RequestId) ? "adm-invalid" : error.RequestId;
        EnsureRequestId(requestId);
        return JsonSerializer.Serialize(new
        {
            version = Version,
            kind = "error",
            requestId,
            error = new { code = error.Code, messageKey = error.MessageKey },
        }, WebJsonOptions);
    }

    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static bool IsAllowedTopLevelSource(string? source) =>
        Uri.TryCreate(source, UriKind.Absolute, out var candidate) &&
        string.Equals(candidate.Scheme, LocalWebViewPolicy.StartUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(candidate.Host, LocalWebViewPolicy.StartUri.Host, StringComparison.OrdinalIgnoreCase) &&
        candidate.Port == LocalWebViewPolicy.StartUri.Port &&
        string.Equals(candidate.AbsolutePath, LocalWebViewPolicy.StartUri.AbsolutePath, StringComparison.Ordinal) &&
        string.IsNullOrEmpty(candidate.Query) &&
        string.IsNullOrEmpty(candidate.Fragment);

    private static void EnsureMessageSize(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > MaxMessageBytes)
            throw new LocalChannelProtocolException("message_too_large", "errors.localChannel.messageTooLarge", null);
    }

    private static void EnsureTopLevelSource(string? source)
    {
        if (!IsAllowedTopLevelSource(source))
            throw new LocalChannelProtocolException("invalid_request", "errors.localChannel.invalidRequest", null);
    }

    private static void RequireObject(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new LocalChannelProtocolException("invalid_request", "errors.localChannel.invalidRequest", null);
    }

    private static void RequireExactProperties(JsonElement root, params string[] expected)
    {
        var properties = root.EnumerateObject().ToArray();
        var names = properties.Select(property => property.Name).ToArray();
        if (properties.Length != expected.Length || names.Distinct(StringComparer.Ordinal).Count() != expected.Length ||
            expected.Any(name => !names.Contains(name, StringComparer.Ordinal)))
            throw new LocalChannelProtocolException("invalid_request", "errors.localChannel.invalidRequest", ReadOptionalRequestId(root));
    }

    private static int ReadVersion(JsonElement root)
    {
        if (!root.TryGetProperty("version", out var version) || !version.TryGetInt32(out var value))
            throw new LocalChannelProtocolException("invalid_request", "errors.localChannel.invalidRequest", ReadOptionalRequestId(root));
        return value;
    }

    private static string ReadString(JsonElement root, string property, string code, string messageKey)
    {
        if (!root.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new LocalChannelProtocolException(code, messageKey, ReadOptionalRequestId(root));
        return value.GetString()!;
    }

    private static string ReadRequestId(JsonElement root)
    {
        var value = ReadString(root, "requestId", "invalid_request", "errors.localChannel.invalidRequest");
        EnsureRequestId(value);
        return value;
    }

    private static string ReadOperation(JsonElement root, string requestId)
    {
        var operation = ReadString(root, "operation", "invalid_request", "errors.localChannel.invalidRequest");
        if (operation.Length > MaxOperationLength || !SafeOperation.IsMatch(operation))
            throw new LocalChannelProtocolException("invalid_request", "errors.localChannel.invalidRequest", requestId);
        return operation;
    }

    private static string? ReadOptionalRequestId(JsonElement root) =>
        root.TryGetProperty("requestId", out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static void EnsureRequestId(string? value)
    {
        if (value is null || value.Length > MaxRequestIdLength || !SafeRequestId.IsMatch(value))
            throw new LocalChannelProtocolException("invalid_request", "errors.localChannel.invalidRequest", null);
    }
}

public interface ILocalChannelMessage
{
}
public sealed record LocalChannelRequest(string RequestId, string Operation, JsonElement Payload) : ILocalChannelMessage;
public sealed record LocalChannelResponse(string RequestId, object? Result) : ILocalChannelMessage;
public sealed record LocalChannelError(string? RequestId, string Code, string MessageKey) : ILocalChannelMessage;

public sealed class LocalChannelProtocolException(string code, string messageKey, string? requestId) : Exception
{
    public string Code { get; } = code;
    public string MessageKey { get; } = messageKey;
    public string? RequestId { get; } = requestId;
}
