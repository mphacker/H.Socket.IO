using System;
using System.Threading;
using System.Threading.Tasks;
using H.WebSockets.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Engine.IO.IntegrationTests
{
    internal class BaseTests
    {
        public static async Task ConnectToChatBaseTestAsync(string url, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_1 || NETCOREAPP3_0
            await using var client = new EngineIoClient("socket.io");
#else
            using var client = new EngineIoClient("socket.io");
#endif

            client.MessageReceived += (sender, args) => Console.WriteLine($"MessageReceived: {args.Value}");
            client.ExceptionOccurred += (sender, args) => Console.WriteLine($"ExceptionOccurred: {args.Value}");
            client.Opened += (sender, args) => Console.WriteLine($"Opened: {args.Value}");
            client.Closed += (sender, args) => Console.WriteLine($"Closed. Reason: {args.Reason}, Status: {args.Status:G}");

            var results = await client.WaitAllEventsAsync<EventArgs>(async () =>
            {
                Console.WriteLine("# Before OpenAsync");

                await client.OpenAsync(new Uri(url), 10);

                Console.WriteLine("# Before Delay");

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

                Console.WriteLine("# Before CloseAsync");

                await client.CloseAsync(cancellationToken);

                Console.WriteLine("# After CloseAsync");
            }, cancellationToken, nameof(client.Opened), nameof(client.Closed));

            Console.WriteLine();
            Console.WriteLine($"WebSocket State: {client.WebSocketClient.Socket.State}");
            Console.WriteLine($"WebSocket CloseStatus: {client.WebSocketClient.Socket.CloseStatus}");
            Console.WriteLine($"WebSocket CloseStatusDescription: {client.WebSocketClient.Socket.CloseStatusDescription}");

            foreach (var pair in results)
            {
                Assert.IsNotNull(pair.Value, $"Client event(\"{pair.Key}\") did not happen");
            }
        }
    }
}
