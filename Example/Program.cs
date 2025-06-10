using System;
using System.Threading.Tasks;
using GQLSubscription;

namespace Example;

public static class Program {
    private const string SubscriptionQuery =
        """
        subscription {
            getData {
                id
                entry
                createDate
                updateDate
                status
                object {
                    id
                    code
                    name
                    type
                }
            }
        }
        """;

    public static async Task Main(string[] args) {
        await using var subscription = new GraphQLSubscription("ws://localhost/graphql", SubscriptionQuery);

        // Set authentication cookie if required
        subscription.SetCookie("session-token", "af610837-80c7-47c5-9fc1-4e3f8e185a9c");

        // Set custom headers if required
        subscription.SetHeader("example-header-entry", "example-value");

        // Handle subscription errors
        subscription.OnError += async error => {
            Console.WriteLine($"Subscription Error Type: {error.Type}");
            foreach (var message in error.Messages) {
                Console.WriteLine($"Subscription Error Message: {message}");
            }

            await Task.CompletedTask;
        };

        // Handle subscription data
        subscription.OnReceived += async response => {
            try {
                var data = response.GetDataFieldAs<Data>("getData");
                Console.WriteLine($"Received: {data?.Entry ?? "null"}");
            }
            catch (Exception ex) {
                Console.WriteLine($"Error processing data: {ex.Message}");
            }

            await Task.CompletedTask;
        };

        // Handle subscription completion
        subscription.OnCompleted += async () => {
            Console.WriteLine("Subscription completed");
            await Task.CompletedTask;
        };

        try {
            Console.WriteLine("Connecting to GraphQL subscription...");
            await subscription.ConnectAsync();

            Console.WriteLine("Press any key to disconnect");
            Console.ReadKey(true);

            Console.WriteLine("Disconnecting...");
            await subscription.DisconnectAsync();
        }
        catch (Exception ex) {
            Console.WriteLine($"Connection Error: {ex.Message}");
        }
    }
}