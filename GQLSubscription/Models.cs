using Newtonsoft.Json;

namespace GQLSubscription {
    public class Message<T> where T : class {
        [JsonProperty("id")] public string ID { get; set; }

        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("payload")] public T Payload { get; set; }
    }

    public class ConnectionMessage {
        [JsonProperty("message")] public string Message { get; set; }
    }

    public class Payload {
        [JsonProperty("operationName")] public string OperationName { get; set; }

        [JsonProperty("query")] public string Query { get; set; }

        [JsonProperty("variables")] public dynamic Variables { get; set; }
    }

    public class Error {
        public Error(GQLSubscriptionErrorType type, params string[] messages) {
            Type     = type;
            Messages = messages;
        }

        public GQLSubscriptionErrorType Type { get; }

        public string[] Messages { get; set; }
    }

    public enum GQLSubscriptionErrorType {
        Connection, Subscription, UnhandledResponseType, GQLError,
        Stop, Disconnect
    }
}