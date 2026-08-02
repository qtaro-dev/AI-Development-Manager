using System.Text.Json.Serialization;

namespace Adm.Server.Host.Errors;

public sealed record AdmProblemDetails(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("detail")] string Detail,
    [property: JsonPropertyName("instance")] string Instance,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("messageKey")] string MessageKey,
    [property: JsonPropertyName("inputRetained")] bool InputRetained,
    [property: JsonPropertyName("retryable")] bool Retryable,
    [property: JsonPropertyName("nextAction")] string NextAction,
    [property: JsonPropertyName("traceId")] string TraceId);
