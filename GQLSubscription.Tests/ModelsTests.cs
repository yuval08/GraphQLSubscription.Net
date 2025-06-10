using System.Text.Json;

namespace GQLSubscription.Tests;

[TestFixture]
public class ModelsTests {
    [Test]
    public void Message_ShouldSerializeAndDeserializeCorrectly() {
        var message = new Message<Payload> {
            Id   = "test-id",
            Type = "test-type",
            Payload = new Payload {
                Query         = "subscription { test }",
                OperationName = "TestOperation",
                Variables     = new { testVar = "value" }
            }
        };

        var json         = JsonSerializer.Serialize(message);
        var deserialized = JsonSerializer.Deserialize<Message<Payload>>(json);

        Assert.Multiple(() => {
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized!.Id, Is.EqualTo("test-id"));
            Assert.That(deserialized.Type, Is.EqualTo("test-type"));
            Assert.That(deserialized.Payload, Is.Not.Null);
            Assert.That(deserialized.Payload!.Query, Is.EqualTo("subscription { test }"));
            Assert.That(deserialized.Payload.OperationName, Is.EqualTo("TestOperation"));
        });
    }

    [Test]
    public void Message_WithNullPayload_ShouldSerializeCorrectly() {
        var message = new Message<Payload> {
            Id      = "test-id",
            Type    = "test-type",
            Payload = null
        };

        var json         = JsonSerializer.Serialize(message);
        var deserialized = JsonSerializer.Deserialize<Message<Payload>>(json);

        Assert.Multiple(() => {
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized!.Id, Is.EqualTo("test-id"));
            Assert.That(deserialized.Type, Is.EqualTo("test-type"));
            Assert.That(deserialized.Payload, Is.Null);
        });
    }

    [Test]
    public void ConnectionMessage_ShouldSerializeAndDeserializeCorrectly() {
        var connectionMessage = new ConnectionMessage {
            Message = "Connection acknowledged"
        };

        var json         = JsonSerializer.Serialize(connectionMessage);
        var deserialized = JsonSerializer.Deserialize<ConnectionMessage>(json);

        Assert.Multiple(() => {
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized!.Message, Is.EqualTo("Connection acknowledged"));
        });
    }

    [Test]
    public void Payload_ShouldSerializeAndDeserializeCorrectly() {
        var payload = new Payload {
            Query         = "subscription { messageAdded { id content } }",
            OperationName = "MessageSubscription",
            Variables     = new { roomId = 123 }
        };

        var json         = JsonSerializer.Serialize(payload);
        var deserialized = JsonSerializer.Deserialize<Payload>(json);

        Assert.Multiple(() => {
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized!.Query, Is.EqualTo("subscription { messageAdded { id content } }"));
            Assert.That(deserialized.OperationName, Is.EqualTo("MessageSubscription"));
            Assert.That(deserialized.Variables, Is.Not.Null);
        });
    }

    [Test]
    public void Payload_WithMinimalData_ShouldWork() {
        var payload = new Payload {
            Query = "subscription { test }"
        };

        var json         = JsonSerializer.Serialize(payload);
        var deserialized = JsonSerializer.Deserialize<Payload>(json);

        Assert.Multiple(() => {
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized!.Query, Is.EqualTo("subscription { test }"));
            Assert.That(deserialized.OperationName, Is.Null);
            Assert.That(deserialized.Variables, Is.Null);
        });
    }

    [Test]
    public void GqlSubscriptionError_ShouldCreateWithTypeAndMessages() {
        var error = new GqlSubscriptionError(GqlSubscriptionErrorType.Connection, "Connection failed", "Timeout occurred");

        Assert.Multiple(() => {
            Assert.That(error.Type, Is.EqualTo(GqlSubscriptionErrorType.Connection));
            Assert.That(error.Messages, Has.Length.EqualTo(2));
            Assert.That(error.Messages[0], Is.EqualTo("Connection failed"));
            Assert.That(error.Messages[1], Is.EqualTo("Timeout occurred"));
        });
    }

    [Test]
    public void GqlSubscriptionError_WithSingleMessage_ShouldWork() {
        var error = new GqlSubscriptionError(GqlSubscriptionErrorType.GqlError, "GraphQL syntax error");

        Assert.Multiple(() => {
            Assert.That(error.Type, Is.EqualTo(GqlSubscriptionErrorType.GqlError));
            Assert.That(error.Messages, Has.Length.EqualTo(1));
            Assert.That(error.Messages[0], Is.EqualTo("GraphQL syntax error"));
        });
    }

    [Test]
    public void GqlSubscriptionError_WithNoMessages_ShouldWork() {
        var error = new GqlSubscriptionError(GqlSubscriptionErrorType.Stop);

        Assert.Multiple(() => {
            Assert.That(error.Type, Is.EqualTo(GqlSubscriptionErrorType.Stop));
            Assert.That(error.Messages, Is.Not.Null);
            Assert.That(error.Messages, Has.Length.EqualTo(0));
        });
    }

    [Test]
    public void GqlSubscriptionErrorType_ShouldHaveAllExpectedValues() {
        var expectedValues = new[] {
            GqlSubscriptionErrorType.Connection,
            GqlSubscriptionErrorType.Subscription,
            GqlSubscriptionErrorType.UnhandledResponseType,
            GqlSubscriptionErrorType.GqlError,
            GqlSubscriptionErrorType.Stop,
            GqlSubscriptionErrorType.Disconnect
        };

        foreach (var expectedValue in expectedValues) {
            Assert.That(Enum.IsDefined(typeof(GqlSubscriptionErrorType), expectedValue), Is.True);
        }
    }
}