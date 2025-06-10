namespace GQLSubscription.Tests;

[TestFixture]
public class SubscriptionTests {
    private GraphQLSubscription? _subscription;

    [TearDown]
    public async Task TearDown() {
        if (_subscription != null) {
            await _subscription.DisposeAsync();
        }
    }

    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance() {
        var subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");

        Assert.That(subscription, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithVariables_ShouldCreateInstance() {
        var variables    = new { roomId = 123 };
        var subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }", variables);

        Assert.That(subscription, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithNullUrl_ShouldThrowArgumentException() {
        Assert.Throws<ArgumentNullException>(() =>
            new GraphQLSubscription(null!, "subscription { test }"));
    }

    [Test]
    public void Constructor_WithEmptyUrl_ShouldThrowArgumentException() {
        Assert.Throws<ArgumentException>(() =>
            new GraphQLSubscription("", "subscription { test }"));
    }

    [Test]
    public void Constructor_WithWhitespaceUrl_ShouldThrowArgumentException() {
        Assert.Throws<ArgumentException>(() =>
            new GraphQLSubscription("   ", "subscription { test }"));
    }

    [Test]
    public void Constructor_WithNullQuery_ShouldThrowArgumentException() {
        Assert.Throws<ArgumentNullException>(() =>
            new GraphQLSubscription("ws://localhost:4000/graphql", null!));
    }

    [Test]
    public void Constructor_WithEmptyQuery_ShouldThrowArgumentException() {
        Assert.Throws<ArgumentException>(() =>
            new GraphQLSubscription("ws://localhost:4000/graphql", ""));
    }

    [Test]
    public void Constructor_WithWhitespaceQuery_ShouldThrowArgumentException() {
        Assert.Throws<ArgumentException>(() =>
            new GraphQLSubscription("ws://localhost:4000/graphql", "   "));
    }

    [Test]
    public void Constructor_WithInvalidUrl_ShouldThrowUriFormatException() {
        Assert.Throws<UriFormatException>(() =>
            new GraphQLSubscription("invalid-url", "subscription { test }"));
    }

    [Test]
    public void SetCookie_WithValidParameters_ShouldNotThrow() {
        _subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");

        Assert.DoesNotThrow(() => _subscription.SetCookie("sessionId", "abc123"));
    }

    [Test]
    public void SetCookie_WithNullName_ShouldThrowArgumentException() {
        _subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");

        Assert.Throws<ArgumentNullException>(() => _subscription.SetCookie(null!, "value"));
    }

    [Test]
    public void SetCookie_WithEmptyName_ShouldThrowArgumentException() {
        _subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");

        Assert.Throws<ArgumentException>(() => _subscription.SetCookie("", "value"));
    }

    [Test]
    public void SetCookie_WithNullValue_ShouldThrowArgumentException() {
        _subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");

        Assert.Throws<ArgumentNullException>(() => _subscription.SetCookie("name", null!));
    }

    [Test]
    public void SetCookie_WithEmptyValue_ShouldThrowArgumentException() {
        _subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");

        Assert.Throws<ArgumentException>(() => _subscription.SetCookie("name", ""));
    }

    [Test]
    public async Task SetCookie_AfterDispose_ShouldThrowObjectDisposedException() {
        _subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");
        await _subscription.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => _subscription.SetCookie("name", "value"));
    }

    [Test]
    public void SetHeader_WithValidParameters_ShouldNotThrow() {
        _subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");

        Assert.DoesNotThrow(() => _subscription.SetHeader("Authorization", "Bearer token123"));
    }

    [Test]
    public void SetHeader_WithNullName_ShouldThrowArgumentException() {
        _subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");

        Assert.Throws<ArgumentNullException>(() => _subscription.SetHeader(null!, "value"));
    }

    [Test]
    public void SetHeader_WithEmptyName_ShouldThrowArgumentException() {
        _subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");

        Assert.Throws<ArgumentException>(() => _subscription.SetHeader("", "value"));
    }

    [Test]
    public void SetHeader_WithNullValue_ShouldThrowArgumentException() {
        _subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");

        Assert.Throws<ArgumentNullException>(() => _subscription.SetHeader("name", null!));
    }

    [Test]
    public void SetHeader_WithEmptyValue_ShouldThrowArgumentException() {
        _subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");

        Assert.Throws<ArgumentException>(() => _subscription.SetHeader("name", ""));
    }

    [Test]
    public async Task SetHeader_AfterDispose_ShouldThrowObjectDisposedException() {
        _subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");
        await _subscription.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => _subscription.SetHeader("name", "value"));
    }

    [Test]
    public async Task ConnectAsync_AfterDispose_ShouldThrowObjectDisposedException() {
        _subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");
        await _subscription.DisposeAsync();

        Assert.ThrowsAsync<ObjectDisposedException>(async () => await _subscription.ConnectAsync());
    }

    [Test]
    public async Task DisposeAsync_CalledMultipleTimes_ShouldNotThrow() {
        _subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");

        await _subscription.DisposeAsync();
        Assert.DoesNotThrowAsync(async () => await _subscription.DisposeAsync());
    }

    [Test]
    public async Task DisconnectAsync_CalledMultipleTimes_ShouldNotThrow() {
        _subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");

        await _subscription.DisconnectAsync();
        Assert.DoesNotThrowAsync(async () => await _subscription.DisconnectAsync());
    }

    [Test]
    public void EventHandlers_ShouldBeSettable() {
        _subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");
        var receivedCalled  = false;
        var errorCalled     = false;
        var completedCalled = false;

        _subscription.OnReceived += async response => {
            receivedCalled = true;
            await Task.CompletedTask;
        };
        _subscription.OnError += async error => {
            errorCalled = true;
            await Task.CompletedTask;
        };
        _subscription.OnCompleted += async () => {
            completedCalled = true;
            await Task.CompletedTask;
        };

        Assert.DoesNotThrow(() => {
            _subscription.OnReceived  += async response => { await Task.CompletedTask; };
            _subscription.OnError     += async error => { await Task.CompletedTask; };
            _subscription.OnCompleted += async () => { await Task.CompletedTask; };
        });
    }

    [Test]
    public async Task ConnectAsync_WithCancellationToken_ShouldRespectCancellation() {
        _subscription = new GraphQLSubscription("ws://localhost:9999/graphql", "subscription { test }");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var ex = Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await _subscription.ConnectAsync(cts.Token));
        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public void SetCookie_MultipleCookies_ShouldNotThrow() {
        _subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");

        Assert.DoesNotThrow(() => {
            _subscription.SetCookie("cookie1", "value1");
            _subscription.SetCookie("cookie2", "value2");
            _subscription.SetCookie("cookie3", "value3");
        });
    }

    [Test]
    public void SetHeader_MultipleHeaders_ShouldNotThrow() {
        _subscription = new GraphQLSubscription("ws://localhost:4000/graphql", "subscription { test }");

        Assert.DoesNotThrow(() => {
            _subscription.SetHeader("Authorization", "Bearer token");
            _subscription.SetHeader("X-Custom-Header", "custom-value");
            _subscription.SetHeader("User-Agent", "TestClient/1.0");
        });
    }
}