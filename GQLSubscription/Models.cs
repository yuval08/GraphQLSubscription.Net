using System.Text.Json.Serialization;

namespace GQLSubscription;

public sealed class Message<T> where T : class {
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("payload")]
    public T? Payload { get; init; }
}

public sealed class ConnectionMessage {
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}

public sealed class Payload {
    [JsonPropertyName("operationName")]
    public string? OperationName { get; init; }

    [JsonPropertyName("query")]
    public required string Query { get; init; }

    [JsonPropertyName("variables")]
    public object? Variables { get; init; }
}

public sealed class GqlSubscriptionError(GqlSubscriptionErrorType type, params string[] messages) {
    public GqlSubscriptionErrorType Type     { get; } = type;
    public string[]                 Messages { get; } = messages;
}

public enum GqlSubscriptionErrorType {
    Connection,
    Subscription,
    UnhandledResponseType,
    GqlError,
    Stop,
    Disconnect
}