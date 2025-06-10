using System.Text.Json;
using System.Text.Json.Serialization;

namespace GQLSubscription.Tests;

[TestFixture]
public class ReceiveTests {
    [Test]
    public void SubscriptionResponse_GetDataFieldAs_ShouldDeserializeCorrectly() {
        var testData = new {
            messageAdded = new {
                id        = "123",
                content   = "Hello World",
                timestamp = "2023-01-01T00:00:00Z"
            },
            userCount = 42
        };

        var jsonElement = JsonSerializer.SerializeToElement(testData);
        var response    = new SubscriptionResponse(jsonElement);

        var messageAdded = response.GetDataFieldAs<TestMessage>("messageAdded");

        Assert.Multiple(() => {
            Assert.That(messageAdded, Is.Not.Null);
            Assert.That(messageAdded!.Id, Is.EqualTo("123"));
            Assert.That(messageAdded.Content, Is.EqualTo("Hello World"));
            Assert.That(messageAdded.Timestamp, Is.EqualTo("2023-01-01T00:00:00Z"));
        });
    }

    [Test]
    public void SubscriptionResponse_GetDataFieldAs_WithNonExistentField_ShouldThrowArgumentException() {
        var testData    = new { existingField = "value" };
        var jsonElement = JsonSerializer.SerializeToElement(testData);
        var response    = new SubscriptionResponse(jsonElement);

        var ex = Assert.Throws<ArgumentException>(() =>
            response.GetDataFieldAs<TestMessage>("nonExistentField"));

        Assert.Multiple(() => {
            Assert.That(ex.Message, Does.Contain("Field 'nonExistentField' not found in response data"));
            Assert.That(ex.ParamName, Is.EqualTo("fieldName"));
        });
    }

    [Test]
    public void SubscriptionResponse_GetDataFieldAs_WithInvalidJson_ShouldThrowJsonException() {
        var testData    = new { invalidField = "not a valid object for TestMessage" };
        var jsonElement = JsonSerializer.SerializeToElement(testData);
        var response    = new SubscriptionResponse(jsonElement);

        Assert.Throws<JsonException>(() =>
            response.GetDataFieldAs<TestMessage>("invalidField"));
    }

    [Test]
    public void SubscriptionResponse_GetDataFieldAs_WithNullValue_ShouldReturnNull() {
        var testData    = new { nullField = (object?)null };
        var jsonElement = JsonSerializer.SerializeToElement(testData);
        var response    = new SubscriptionResponse(jsonElement);

        var result = response.GetDataFieldAs<TestMessage>("nullField");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void SubscriptionResponse_GetRawData_ShouldReturnJsonString() {
        var testData = new {
            messageAdded = new {
                id      = "123",
                content = "Hello World"
            }
        };

        var jsonElement = JsonSerializer.SerializeToElement(testData);
        var response    = new SubscriptionResponse(jsonElement);

        var rawData = response.GetRawData();

        Assert.Multiple(() => {
            Assert.That(rawData, Is.Not.Null);
            Assert.That(rawData, Does.Contain("messageAdded"));
            Assert.That(rawData, Does.Contain("Hello World"));
            Assert.That(rawData, Does.Contain("123"));
        });

        var deserializedBack = JsonSerializer.Deserialize<JsonElement>(rawData);
        Assert.That(deserializedBack.GetProperty("messageAdded").GetProperty("id").GetString(), Is.EqualTo("123"));
    }

    [Test]
    public void SubscriptionResponse_GetRawData_WithComplexData_ShouldPreserveStructure() {
        var testData = new {
            users = new[] {
                new { id = 1, name = "John" },
                new { id = 2, name = "Jane" }
            },
            metadata = new {
                total   = 2,
                hasMore = false
            }
        };

        var jsonElement = JsonSerializer.SerializeToElement(testData);
        var response    = new SubscriptionResponse(jsonElement);

        var rawData          = response.GetRawData();
        var deserializedBack = JsonSerializer.Deserialize<JsonElement>(rawData);

        Assert.Multiple(() => {
            Assert.That(deserializedBack.GetProperty("users").GetArrayLength(), Is.EqualTo(2));
            Assert.That(deserializedBack.GetProperty("users")[0].GetProperty("name").GetString(), Is.EqualTo("John"));
            Assert.That(deserializedBack.GetProperty("metadata").GetProperty("total").GetInt32(), Is.EqualTo(2));
            Assert.That(deserializedBack.GetProperty("metadata").GetProperty("hasMore").GetBoolean(), Is.False);
        });
    }

    [Test]
    public void SubscriptionResponse_GetDataFieldAs_WithDifferentTypes_ShouldWork() {
        var testData = new {
            stringField = "test string",
            arrayField  = new[] { 1, 2, 3 },
            objectField = new { id = 42, enabled = true }
        };

        var jsonElement = JsonSerializer.SerializeToElement(testData);
        var response    = new SubscriptionResponse(jsonElement);

        var stringResult = response.GetDataFieldAs<string>("stringField");
        var arrayResult  = response.GetDataFieldAs<int[]>("arrayField");
        var objectResult = response.GetDataFieldAs<TestObject>("objectField");

        Assert.Multiple(() => {
            Assert.That(stringResult, Is.EqualTo("test string"));
            Assert.That(arrayResult, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.Id, Is.EqualTo(42));
            Assert.That(objectResult.Enabled, Is.True);
        });
    }

    private class TestMessage {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }
    }

    private class TestObject {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
    }
}