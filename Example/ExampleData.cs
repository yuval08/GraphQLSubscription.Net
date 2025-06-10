using System;
using System.Text.Json.Serialization;

namespace Example;

public sealed class Data {
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("object")]
    public DataObject? Object { get; init; }

    [JsonPropertyName("entry")]
    public required string Entry { get; init; }

    [JsonPropertyName("createDate")]
    public DateTime CreateDate { get; init; }

    [JsonPropertyName("updateDate")]
    public DateTime UpdateDate { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }
}

public sealed class DataObject {
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }
}