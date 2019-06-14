using System;
using GQLSubscription;

namespace Example {
    public static class Program {
        const string qry = @"subscription {
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
}";

        public static void Main(string[] args) {
            try {
                //Create a subscription object
                var subscription = new Subscription("ws://localhost/graphql", qry);
                
                //Set here your session token or jwt token if required as a cookie
                subscription.SetCookie("session-token", "af610837-80c7-47c5-9fc1-4e3f8e185a9c");
                
                //Set here you header required entries
                subscription.SetHeader("example-header-entry", "example-value");
                
                //Catch any error that can be generated from the subscription
                subscription.OnError += errors => {
                    Console.WriteLine($"Subscription Error Type: {errors.Type}");
                    foreach (string errorsMessage in errors.Messages) {
                        Console.WriteLine($"Subscription Error Message: {errorsMessage}");
                    }
                };
                //Catch when the subscription is getting a response
                subscription.OnResponse += response => Console.WriteLine(response.GetDataFieldAs<Data>("getData").Entry);
                subscription.Connect();

                Console.Write("Press any key to disconnection");
                Console.ReadKey(true);
                subscription.Disconnect();
            } catch (Exception ex) {
                Console.WriteLine($"Connection Error: {ex.Message}");
            }
        }
    }
}