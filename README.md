# GraphQLSubscription.Net

A modern .NET library for GraphQL WebSocket subscriptions that provides a simple and efficient way to handle real-time GraphQL data over WebSocket connections.

## Features

- **WebSocket-based GraphQL subscriptions** using the standard GraphQL over WebSocket protocol
- **Modern .NET 9.0** support with nullable reference types
- **Async/await pattern** for all operations
- **Authentication support** via cookies and custom headers
- **Comprehensive error handling** with detailed error information
- **Type-safe data access** with generic methods
- **Automatic connection management** with proper disposal pattern
- **Event-driven architecture** for handling data, errors, and completion

## Installation

```bash
dotnet add package GraphQLSubscription.Net
```

## Quick Start

```csharp
using GQLSubscription;

const string query = """
    subscription {
        getData {
            id
            name
            status
        }
    }
    """;

await using var subscription = new GraphQLSubscription("ws://localhost/graphql", query);

// Handle received data
subscription.OnReceived += async response => {
    var data = response.GetDataFieldAs<MyDataType>("getData");
    Console.WriteLine($"Received: {data?.Name}");
};

// Handle errors
subscription.OnError += async error => {
    Console.WriteLine($"Error: {error.Type}");
    foreach (var message in error.Messages) {
        Console.WriteLine($"Message: {message}");
    }
};

// Connect and start receiving data
await subscription.ConnectAsync();

// Keep connection alive
Console.ReadKey();

// Clean disconnect
await subscription.DisconnectAsync();
```

## Authentication

### Using Cookies
```csharp
subscription.SetCookie("session-token", "your-token-here");
```

### Using Custom Headers
```csharp
subscription.SetHeader("Authorization", "Bearer your-token-here");
subscription.SetHeader("X-API-Key", "your-api-key");
```

## Error Handling

The library provides comprehensive error handling through the `OnError` event:

```csharp
subscription.OnError += async error => {
    Console.WriteLine($"Error Type: {error.Type}");
    
    // Handle different error types
    switch (error.Type) {
        case "connection_error":
            // Handle connection issues
            break;
        case "validation_error":
            // Handle GraphQL validation errors
            break;
        default:
            // Handle other errors
            break;
    }
    
    // Access detailed error messages
    foreach (var message in error.Messages) {
        Console.WriteLine($"Detail: {message}");
    }
};
```

## Data Processing

Extract typed data from subscription responses:

```csharp
subscription.OnReceived += async response => {
    // Extract specific field as typed object
    var userData = response.GetDataFieldAs<User>("user");
    var messageData = response.GetDataFieldAs<Message>("newMessage");
    
    // Access raw JSON if needed
    var rawData = response.Data;
};
```

## Connection Lifecycle

```csharp
await using var subscription = new GraphQLSubscription(url, query);

// Set up event handlers before connecting
subscription.OnReceived += HandleData;
subscription.OnError += HandleError;
subscription.OnCompleted += async () => Console.WriteLine("Completed");

try {
    await subscription.ConnectAsync();
    
    // Keep connection alive for desired duration
    await Task.Delay(TimeSpan.FromMinutes(5));
    
    await subscription.DisconnectAsync();
}
catch (Exception ex) {
    Console.WriteLine($"Connection failed: {ex.Message}");
}
```

## Requirements

- **.NET 9.0** or later
- **WebSocket-enabled GraphQL server** supporting the GraphQL over WebSocket protocol

## License

This project is licensed under the GPL-3.0 License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit issues, feature requests, or pull requests.

## Changelog

### Version 0.0.2
- Upgraded to .NET 9.0 and latest package dependencies
- Added comprehensive unit tests
- Improved error handling and connection management
- Enhanced code quality with nullable reference types and warnings as errors
