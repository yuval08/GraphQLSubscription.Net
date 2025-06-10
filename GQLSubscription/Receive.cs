using System;
using System.Text.Json;

namespace GQLSubscription;

public sealed class SubscriptionResponse(JsonElement data) {
    private JsonElement Data { get; } = data;

    /// <summary>
    /// Convert the returned data object from GraphQL subscription to a deserialized class
    /// </summary>
    /// <param name="fieldName">The field name returned from the GraphQL subscription data object</param>
    /// <typeparam name="TOut">The class type to be deserialized</typeparam>
    /// <returns>A deserialized object from the GraphQL subscription data</returns>
    /// <exception cref="JsonException">Thrown when deserialization fails</exception>
    /// <exception cref="ArgumentException">Thrown when field name is not found</exception>
    public TOut? GetDataFieldAs<TOut>(string fieldName) where TOut : class {
        if (!Data.TryGetProperty(fieldName, out var fieldValue)) {
            throw new ArgumentException($"Field '{fieldName}' not found in response data", nameof(fieldName));
        }

        return JsonSerializer.Deserialize<TOut>(fieldValue.GetRawText());
    }

    /// <summary>
    /// Gets the raw JSON data as a string
    /// </summary>
    /// <returns>The raw JSON string</returns>
    public string GetRawData() => Data.GetRawText();
}