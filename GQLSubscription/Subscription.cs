using System;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GQLSubscription {
    /// <summary>
    /// The Subscription class to connect and run a GraphQL subscription
    /// </summary>
    public class Subscription {
        private readonly ClientWebSocket socket;
        private readonly Uri             uri;
        private readonly string          query;
        private readonly dynamic         variables;
        private readonly byte[]          buffer = new byte[1024 * 1024];

        private bool isRunning;

        private const string ConnectionInitialize = "connection_init";
        private const string ConnectionOk         = "connection_ack";
        private const string ConnectionError      = "connection_error";
        private const string ConnectionKeepAlive  = "ka";

        private const string CommandTerminate = "connection_terminate";
        private const string CommandStart     = "start";
        private const string CommandStop      = "stop";

        private const string ReturnTypeData     = "data";
        private const string ReturnTypeError    = "error";
        private const string ReturnTypeComplete = "complete";

        public Action OnComplete;

        public Action<Receive> OnReceive;

        public Action<Error> OnError;

        /// <summary>
        /// Constructor for the subscription class
        /// </summary>
        /// <param name="url">A valid url to connection to a GraphQL server websocket. Example: ws://10.10.10.10/query</param>
        /// <param name="query">A valid graphql query</param>
        /// <param name="variables">An object with the required variables for the query</param>
        public Subscription(string url, string query, dynamic variables = null) {
            uri            = new Uri(url);
            this.query     = query;
            this.variables = variables;
            socket         = new ClientWebSocket();
            socket.Options.SetRequestHeader("Upgrade", "websocket");
            socket.Options.AddSubProtocol("graphql-ws");
        }

        /// <summary>
        /// Sets an new cookie for the Web Socket cookies values
        /// </summary>
        /// <param name="name">Cookie name</param>
        /// <param name="value">Cookie value</param>
        public void SetCookie(string name, string value) {
            if (socket.Options.Cookies == null)
                socket.Options.Cookies = new CookieContainer();
            socket.Options.Cookies.Add(uri, new Cookie(name, value));
        }

        /// <summary>
        /// Sets a new parameter for the Web Socket Header entries
        /// </summary>
        /// <param name="name">Header entry name</param>
        /// <param name="value">Header entry value</param>
        public void SetHeader(string name, string value) => socket.Options.SetRequestHeader(name, value);

        public async Task Connect() {
            try {
                await socket.ConnectAsync(uri, default).ConfigureAwait(false);
                if (!IsAlive()) {
                    OnError?.Invoke(new Error(GQLSubscriptionErrorType.Connection, "Could not establish connection"));
                    return;
                }

                await Subscribe();
            } catch (Exception ex) {
                OnError?.Invoke(new Error(GQLSubscriptionErrorType.Connection, ex.Message));
            }
        }

        private async Task Subscribe() {
            try {
                //Send initialize subscription message
                var request       = new {type = ConnectionInitialize};
                var requestString = JsonConvert.SerializeObject(request);
                var requestArray  = new ArraySegment<byte>(Encoding.UTF8.GetBytes(requestString));
                await socket.SendAsync(requestArray, WebSocketMessageType.Text, true, default);
                //Wait for initialize subscription message
                var arraySegment = new ArraySegment<byte>(buffer);
                var result       = await socket.ReceiveAsync(arraySegment, default);
                var resultString = Encoding.UTF8.GetString(arraySegment.Array, 0, result.Count);
                var connResult   = JsonConvert.DeserializeObject<Message<ConnectionMessage>>(resultString);
                switch (connResult.Type) {
                    case ConnectionOk:
                        break;
                    case ConnectionError:
                        OnError?.Invoke(new Error(GQLSubscriptionErrorType.Subscription, connResult.Payload.Message));
                        return;
                    case ConnectionKeepAlive:
                        break;
                }

                Start();
            } catch (Exception ex) {
                OnError?.Invoke(new Error(GQLSubscriptionErrorType.Subscription, ex.Message));
            }
        }

        private bool IsAlive() => socket.State == WebSocketState.Open;

        private async Task Start() {
            if (!IsAlive())
                return;
            //Send start request
            var request       = new Message<Payload> {ID = "1", Type = CommandStart, Payload = new Payload {Query = query, Variables = variables}};
            var requestString = JsonConvert.SerializeObject(request);
            var requestArray  = new ArraySegment<byte>(Encoding.UTF8.GetBytes(requestString));
            await socket.SendAsync(requestArray, WebSocketMessageType.Text, true, default);
            isRunning = true;
            //wait for result
            var arraySegment = new ArraySegment<byte>(buffer);
            while (IsAlive() && isRunning) {
                var result       = await socket.ReceiveAsync(arraySegment, default);
                var resultString = Encoding.UTF8.GetString(arraySegment.Array, 0, result.Count);
                if (string.IsNullOrWhiteSpace(resultString)) continue;
                var json         = JObject.Parse(resultString);
                switch ((string) json["type"]) {
                    case ReturnTypeData:
                        OnReceive?.Invoke(new Receive(json["payload"]["data"]));
                        break;
                    case ReturnTypeError:
                        isRunning = false;
                        OnError?.Invoke(new Error(GQLSubscriptionErrorType.GQLError, json["payload"].ToList().Select(t => (string) t["message"]).ToArray()));
                        Disconnect();
                        break;
                    case ConnectionKeepAlive:
                        break;
                    case ReturnTypeComplete:
                        OnComplete?.Invoke();
                        Disconnect();
                        break;
                    default:
                        isRunning = false;
                        OnError?.Invoke(new Error(GQLSubscriptionErrorType.UnhandledResponseType, new[] {$"Unhandled type: {json["type"]}"}));
                        Disconnect();
                        break;
                }
            }
        }

        public async void Disconnect() {
            try {
                var request       = new {type = CommandTerminate};
                var requestString = JsonConvert.SerializeObject(request);
                var requestArray  = new ArraySegment<byte>(Encoding.UTF8.GetBytes(requestString));
                isRunning = false;
                await socket.SendAsync(requestArray, WebSocketMessageType.Text, true, default);
            } catch (Exception ex) {
                OnError?.Invoke(new Error(GQLSubscriptionErrorType.Disconnect, ex.Message));
            }
        }

//        public async Task Stop() {
//            if (!isRunning)
//                return;
//            try {
//                var request       = new {type = CommandStop};
//                var requestString = JsonConvert.SerializeObject(request);
//                var requestArray  = new ArraySegment<byte>(Encoding.UTF8.GetBytes(requestString));
//                await socket.SendAsync(requestArray, WebSocketMessageType.Text, true, default);
//            } catch (Exception ex) {
//                OnError?.Invoke(new Error(GQLSubscriptionErrorType.Stop, ex.Message));
//            }
//        }
    }
}