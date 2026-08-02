using System.Text.Json;

namespace Adm.Wpf.Bridge;

public static class BridgeProtocol
{
    public const string Version = "1";
    public const string GetHostInfo = "getHostInfo";
    public static IReadOnlySet<string> AllowedOperations { get; } = new HashSet<string>(StringComparer.Ordinal) { GetHostInfo };

    public static BridgeRequest ParseRequest(string json, string? source, Uri allowedOrigin)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 8 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new BridgeProtocolException("invalid_envelope", "Bridgeメッセージの形式が正しくありません。", null);
        var names = root.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        var required = new[] { "version", "messageType", "operation", "requestId", "payload" };
        if (names.Count != required.Length || required.Any(name => !names.Contains(name)))
            throw new BridgeProtocolException("unknown_field", "Bridgeメッセージに許可されていない項目があります。", null);
        if (!IsAllowedSource(source, allowedOrigin))
            throw new BridgeProtocolException("origin_rejected", "許可された画面からのBridgeメッセージではありません。", null);

        var version = root.GetProperty("version").GetString();
        var messageType = root.GetProperty("messageType").GetString();
        var operation = root.GetProperty("operation").GetString();
        var requestId = root.GetProperty("requestId").GetString();
        if (version != Version) throw new BridgeProtocolException("unsupported_version", "対応していないBridgeバージョンです。", requestId);
        if (messageType is not ("request" or "cancel")) throw new BridgeProtocolException("invalid_message_type", "Bridgeメッセージ種別が正しくありません。", requestId);
        if (string.IsNullOrWhiteSpace(operation) || operation != GetHostInfo) throw new BridgeProtocolException("operation_not_allowed", "許可されていないBridge操作です。", requestId);
        if (!IsSafeId(requestId)) throw new BridgeProtocolException("invalid_request_id", "Bridge要求IDが正しくありません。", null);
        if (root.GetProperty("payload").ValueKind != JsonValueKind.Object || root.GetProperty("payload").EnumerateObject().Any())
            throw new BridgeProtocolException("invalid_payload", "Bridge入力データが正しくありません。", requestId);
        return new BridgeRequest(messageType!, operation!, requestId!);
    }

    public static string Success(BridgeRequest request) => JsonSerializer.Serialize(new { version = Version, messageType = "response", operation = request.Operation, requestId = request.RequestId, status = "ok", payload = new { applicationName = "AI Development Manager", bridgeVersion = Version, runtime = "WebView2" } });
    public static string Cancelled(string requestId) => JsonSerializer.Serialize(new { version = Version, messageType = "response", operation = GetHostInfo, requestId, status = "cancelled" });
    public static string Error(string code, string message, string? requestId) => JsonSerializer.Serialize(new { version = Version, messageType = "response", operation = GetHostInfo, requestId = requestId ?? "adm-invalid", status = "error", error = new { code, message, traceId = requestId ?? "adm-invalid" } });

    public static bool IsAllowedSource(string? source, Uri allowedOrigin) =>
        Uri.TryCreate(source, UriKind.Absolute, out var candidate) &&
        string.Equals(candidate.Scheme, allowedOrigin.Scheme, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(candidate.Host, allowedOrigin.Host, StringComparison.OrdinalIgnoreCase) && candidate.Port == allowedOrigin.Port;

    private static bool IsSafeId(string? value) => value is not null && value.Length is >= 1 and <= 64 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}

public sealed record BridgeRequest(string MessageType, string Operation, string RequestId);
public sealed class BridgeProtocolException(string code, string message, string? requestId) : Exception(message)
{
    public string Code { get; } = code;
    public string? RequestId { get; } = requestId;
}
