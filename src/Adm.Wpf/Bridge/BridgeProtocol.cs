using System.Text;
using System.Text.Json;

namespace Adm.Wpf.Bridge;

public static class BridgeProtocol
{
    public const string Version = "1";
    public const string GetHostInfo = "getHostInfo";
    public const int MaxMessageBytes = 16 * 1024;
    public const int MaxJsonDepth = 8;
    public const string InvalidRequestId = "adm-invalid";
    public static IReadOnlySet<string> AllowedOperations { get; } = new HashSet<string>(StringComparer.Ordinal) { GetHostInfo };

    private static readonly string[] RequiredFields = ["version", "messageType", "operation", "requestId", "payload"];

    public static BridgeRequest ParseRequest(string json, string? source, Uri allowedOrigin)
    {
        if (Encoding.UTF8.GetByteCount(json) > MaxMessageBytes)
            throw new BridgeProtocolException("message_too_large", "Bridgeメッセージが大きすぎます。", null);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 64 });
        }
        catch (JsonException)
        {
            throw new BridgeProtocolException("invalid_json", "Bridgeメッセージの形式が正しくありません。", null);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new BridgeProtocolException("invalid_envelope", "Bridgeメッセージの形式が正しくありません。", null);
            if (GetMaxDepth(root) > MaxJsonDepth)
                throw new BridgeProtocolException("max_depth_exceeded", "Bridgeメッセージの入れ子が深すぎます。", null);

            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw new BridgeProtocolException("duplicate_field", "Bridgeメッセージに重複した項目があります。", null);
            }

            if (names.Count != RequiredFields.Length || RequiredFields.Any(name => !names.Contains(name)))
                throw new BridgeProtocolException("unknown_field", "Bridgeメッセージに許可されていない項目があります。", null);
            if (!IsAllowedSource(source, allowedOrigin))
                throw new BridgeProtocolException("origin_rejected", "許可された画面からのBridgeメッセージではありません。", null);

            var requestId = ReadString(root, "requestId", "invalid_request_id", null);
            var version = ReadString(root, "version", "invalid_field_type", requestId);
            var messageType = ReadString(root, "messageType", "invalid_field_type", requestId);
            var operation = ReadString(root, "operation", "invalid_field_type", requestId);

            if (version != Version)
                throw new BridgeProtocolException("unsupported_version", "対応していないBridgeバージョンです。", requestId);
            if (messageType is not ("request" or "cancel"))
                throw new BridgeProtocolException("invalid_message_type", "Bridgeメッセージ種別が正しくありません。", requestId);
            if (operation != GetHostInfo || !AllowedOperations.Contains(operation))
                throw new BridgeProtocolException("operation_not_allowed", "許可されていないBridge操作です。", requestId);
            if (!IsSafeId(requestId))
                throw new BridgeProtocolException("invalid_request_id", "Bridge要求IDが正しくありません。", null);

            var payload = root.GetProperty("payload");
            if (payload.ValueKind != JsonValueKind.Object || payload.EnumerateObject().Any())
                throw new BridgeProtocolException("invalid_payload", "Bridge入力データが正しくありません。", requestId);

            return new BridgeRequest(messageType, operation, requestId);
        }
    }

    public static string Success(BridgeRequest request) => JsonSerializer.Serialize(new { version = Version, messageType = "response", operation = request.Operation, requestId = request.RequestId, status = "ok", payload = new { applicationName = "AI Development Manager", bridgeVersion = Version, runtime = "WebView2" } });
    public static string Cancelled(string requestId) => JsonSerializer.Serialize(new { version = Version, messageType = "response", operation = GetHostInfo, requestId, status = "cancelled" });
    public static string Error(string code, string message, string? requestId) => JsonSerializer.Serialize(new { version = Version, messageType = "response", operation = GetHostInfo, requestId = requestId ?? InvalidRequestId, status = "error", error = new { code, message, traceId = requestId ?? InvalidRequestId } });

    public static bool IsAllowedSource(string? source, Uri allowedOrigin) =>
        Uri.TryCreate(source, UriKind.Absolute, out var candidate) &&
        string.Equals(candidate.Scheme, allowedOrigin.Scheme, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(candidate.Host, allowedOrigin.Host, StringComparison.OrdinalIgnoreCase) && candidate.Port == allowedOrigin.Port;

    private static string ReadString(JsonElement root, string name, string code, string? requestId)
    {
        var value = root.GetProperty(name);
        if (value.ValueKind != JsonValueKind.String || value.GetString() is not { } text)
            throw new BridgeProtocolException(code, "Bridgeメッセージの項目形式が正しくありません。", requestId);
        return text;
    }

    private static int GetMaxDepth(JsonElement element)
    {
        if (element.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            return 1;

        var childDepth = EnumerateChildren(element).Select(GetMaxDepth).DefaultIfEmpty(0).Max();
        return childDepth + 1;
    }

    private static IEnumerable<JsonElement> EnumerateChildren(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
                yield return child;
        }
        else
        {
            foreach (var child in element.EnumerateObject())
                yield return child.Value;
        }
    }

    private static bool IsSafeId(string value) => value.Length is >= 1 and <= 64 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}

public sealed record BridgeRequest(string MessageType, string Operation, string RequestId);
public sealed class BridgeProtocolException(string code, string message, string? requestId) : Exception(message)
{
    public string Code { get; } = code;
    public string? RequestId { get; } = requestId;
}
