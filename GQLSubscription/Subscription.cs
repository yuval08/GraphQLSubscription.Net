using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GQLSubscription;

/// <summary>
/// A modern GraphQL WebSocket subscription client
/// </summary>
public sealed class GraphQLSubscription : IAsyncDisposable {
    private readonly ClientWebSocket         _socket;
    private readonly Uri                     _uri;
    private readonly string                  _query;
    private readonly object?                 _variables;
    private readonly byte[]                  _buffer                  = new byte[1024 * 1024];
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private volatile bool                          _isRunning;
    private volatile bool                          _isDisposed;
    private const    string                        ConnectionInitialize = "connection_init";
    private const    string                        ConnectionOk         = "connection_ack";
    private const    string                        ConnectionError      = "connection_error";
    private const    string                        ConnectionKeepAlive  = "ka";
    private const    string                        CommandTerminate     = "connection_terminate";
    private const    string                        CommandStart         = "start";
    private const    string                        CommandStop          = "stop";
    private const    string                        ReturnTypeData       = "data";
    private const    string                        ReturnTypeError      = "error";
    private const    string                        ReturnTypeComplete   = "complete";
    public event Func<Task>?                       OnCompleted;
    public event Func<SubscriptionResponse, Task>? OnReceived;
    public event Func<GqlSubscriptionError, Task>? OnError;

    /// <summary>
    /// Initializes a new GraphQL WebSocket subscription client
    /// </summary>
    /// <param name="url">WebSocket URL for the GraphQL server (e.g., ws://localhost:4000/graphql)</param>
    /// <param name="query">GraphQL subscription query</param>
    /// <param name="variables">Optional variables for the subscription query</param>
    /// <exception cref="ArgumentException">Thrown when URL is invalid</exception>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    public GraphQLSubscription(string url, string query, object? variables = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        _uri       = new Uri(url);
        _query     = query;
        _variables = variables;
        _socket    = new ClientWebSocket();
        _socket.Options.SetRequestHeader("Upgrade", "websocket");
        _socket.Options.AddSubProtocol("graphql-ws");
    }

    /// <summary>
    /// Sets a cookie for the WebSocket connection
    /// </summary>
    /// <param name="name">Cookie name</param>
    /// <param name="value">Cookie value</param>
    /// <exception cref="ArgumentException">Thrown when name or value is null or empty</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the subscription is disposed</exception>
    public void SetCookie(string name, string value) {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        _socket.Options.Cookies ??= new CookieContainer();
        _socket.Options.Cookies.Add(_uri, new Cookie(name, value));
    }

    /// <summary>
    /// Sets a header for the WebSocket connection
    /// </summary>
    /// <param name="name">Header name</param>
    /// <param name="value">Header value</param>
    /// <exception cref="ArgumentException">Thrown when name or value is null or empty</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the subscription is disposed</exception>
    public void SetHeader(string name, string value) {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        _socket.Options.SetRequestHeader(name, value);
    }

    /// <summary>
    /// Connects to the GraphQL WebSocket server and starts the subscription
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ObjectDisposedException">Thrown when the subscription is disposed</exception>
    public async Task ConnectAsync(CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            _cancellationTokenSource.Token, cancellationToken);

        try {
            await _socket.ConnectAsync(_uri, linkedCts.Token).ConfigureAwait(false);

            if (!IsAlive()) {
                await InvokeOnErrorAsync(new GqlSubscriptionError(
                    GqlSubscriptionErrorType.Connection,
                    "Could not establish WebSocket connection")).ConfigureAwait(false);
                return;
            }

            await SubscribeAsync(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) {
            await InvokeOnErrorAsync(new GqlSubscriptionError(
                GqlSubscriptionErrorType.Connection, ex.Message)).ConfigureAwait(false);
        }
    }

    private async Task SubscribeAsync(CancellationToken cancellationToken) {
        try {
            // Send connection initialization
            var initRequest = new { type = ConnectionInitialize };
            await SendMessageAsync(initRequest, cancellationToken).ConfigureAwait(false);

            // Wait for connection acknowledgment
            var response   = await ReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
            var connResult = JsonSerializer.Deserialize<Message<ConnectionMessage>>(response);

            switch (connResult?.Type) {
                case ConnectionOk:
                    break;
                case ConnectionError:
                    await InvokeOnErrorAsync(new GqlSubscriptionError(
                        GqlSubscriptionErrorType.Subscription,
                        connResult.Payload?.Message ?? "Connection error")).ConfigureAwait(false);
                    return;
                case ConnectionKeepAlive:
                    break;
                default:
                    await InvokeOnErrorAsync(new GqlSubscriptionError(
                        GqlSubscriptionErrorType.Subscription,
                        $"Unexpected connection response: {connResult?.Type}")).ConfigureAwait(false);
                    return;
            }

            await StartSubscriptionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested)) {
            await InvokeOnErrorAsync(new GqlSubscriptionError(
                GqlSubscriptionErrorType.Subscription, ex.Message)).ConfigureAwait(false);
        }
    }

    private bool IsAlive() => _socket.State == WebSocketState.Open;

    private async Task StartSubscriptionAsync(CancellationToken cancellationToken) {
        if (!IsAlive()) return;

        // Send subscription start request
        var startRequest = new Message<Payload> {
            Id   = "1",
            Type = CommandStart,
            Payload = new Payload {
                Query     = _query,
                Variables = _variables
            }
        };

        await SendMessageAsync(startRequest, cancellationToken).ConfigureAwait(false);
        _isRunning = true;

        // Listen for messages
        while (IsAlive() && _isRunning && !cancellationToken.IsCancellationRequested) {
            try {
                var messageJson = await ReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(messageJson)) continue;

                using var document    = JsonDocument.Parse(messageJson);
                var       messageType = document.RootElement.GetProperty("type").GetString();

                switch (messageType) {
                    case ReturnTypeData:
                        if (document.RootElement.TryGetProperty("payload", out var payload) &&
                            payload.TryGetProperty("data", out var data)) {
                            await InvokeOnReceivedAsync(new SubscriptionResponse(data)).ConfigureAwait(false);
                        }

                        break;

                    case ReturnTypeError:
                        _isRunning = false;
                        var errorMessages = ExtractErrorMessages(document.RootElement);
                        await InvokeOnErrorAsync(new GqlSubscriptionError(
                            GqlSubscriptionErrorType.GqlError, errorMessages)).ConfigureAwait(false);
                        await DisconnectAsync().ConfigureAwait(false);
                        break;

                    case ConnectionKeepAlive:
                        break;

                    case ReturnTypeComplete:
                        await InvokeOnCompletedAsync().ConfigureAwait(false);
                        await DisconnectAsync().ConfigureAwait(false);
                        break;

                    default:
                        _isRunning = false;
                        await InvokeOnErrorAsync(new GqlSubscriptionError(
                            GqlSubscriptionErrorType.UnhandledResponseType,
                            $"Unhandled message type: {messageType}")).ConfigureAwait(false);
                        await DisconnectAsync().ConfigureAwait(false);
                        break;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                break;
            }
            catch (Exception ex) {
                await InvokeOnErrorAsync(new GqlSubscriptionError(
                    GqlSubscriptionErrorType.Subscription, ex.Message)).ConfigureAwait(false);
                break;
            }
        }
    }

    /// <summary>
    /// Disconnects from the GraphQL WebSocket server
    /// </summary>
    public async Task DisconnectAsync() {
        if (_isDisposed || !IsAlive()) return;

        try {
            _isRunning = false;
            var terminateRequest = new { type = CommandTerminate };
            await SendMessageAsync(terminateRequest, _cancellationTokenSource.Token).ConfigureAwait(false);

            if (_socket.State == WebSocketState.Open) {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                    "Subscription terminated", _cancellationTokenSource.Token).ConfigureAwait(false);
            }
        }
        catch (Exception ex) {
            await InvokeOnErrorAsync(new GqlSubscriptionError(
                GqlSubscriptionErrorType.Disconnect, ex.Message)).ConfigureAwait(false);
        }
    }

    private async Task SendMessageAsync<T>(T message, CancellationToken cancellationToken) {
        var json    = JsonSerializer.Serialize(message);
        var bytes   = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        await _socket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> ReceiveMessageAsync(CancellationToken cancellationToken) {
        var segment = new ArraySegment<byte>(_buffer);
        var result  = await _socket.ReceiveAsync(segment, cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(_buffer, 0, result.Count);
    }

    private static string[] ExtractErrorMessages(JsonElement root) {
        if (!root.TryGetProperty("payload", out var payload))
            return ["Unknown error"];

        if (payload.ValueKind == JsonValueKind.Array) {
            var messages = new List<string>();
            foreach (var error in payload.EnumerateArray()) {
                if (error.TryGetProperty("message", out var message)) {
                    messages.Add(message.GetString() ?? "Unknown error");
                }
            }

            return messages.ToArray();
        }

        return [payload.GetString() ?? "Unknown error"];
    }

    private async Task InvokeOnReceivedAsync(SubscriptionResponse response) {
        if (OnReceived != null) {
            await OnReceived.Invoke(response).ConfigureAwait(false);
        }
    }

    private async Task InvokeOnErrorAsync(GqlSubscriptionError error) {
        if (OnError != null) {
            await OnError.Invoke(error).ConfigureAwait(false);
        }
    }

    private async Task InvokeOnCompletedAsync() {
        if (OnCompleted != null) {
            await OnCompleted.Invoke().ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync() {
        if (_isDisposed) return;

        _isDisposed = true;
        await DisconnectAsync().ConfigureAwait(false);

        _cancellationTokenSource.Cancel();
        _socket.Dispose();
        _cancellationTokenSource.Dispose();
    }
}