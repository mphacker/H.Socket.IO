using System;
using System.Threading;
using System.Threading.Tasks;
using H.WebSockets.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.WebSockets.IntegrationTests
{
    [TestClass]
    public class RemoteTests
    {
        [TestMethod]
        public async Task WebSocketOrgTest()
        {
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var cancellationToken = tokenSource.Token;

#if NETCOREAPP3_1 || NETCOREAPP3_0
            await using var client = new WebSocketClient();
#else
            using var client = new WebSocketClient();
#endif

            client.TextReceived += (sender, args) => Console.WriteLine($"TextReceived: {args.Value}");
            client.ExceptionOccurred += (sender, args) => Console.WriteLine($"ExceptionOccurred: {args.Value}");
            client.BytesReceived += (sender, args) => Console.WriteLine($"BytesReceived: {args.Value?.Count}");
            client.Connected += (sender, args) => Console.WriteLine("Connected");
            client.Disconnected += (sender, args) => Console.WriteLine($"Disconnected. Reason: {args.Reason}, Status: {args.Status:G}");

            var events = new[] { nameof(client.Connected), nameof(client.Disconnected) };
            await ConnectDisconnectTestAsync(client, events, cancellationToken);
            await ConnectDisconnectTestAsync(client, events, cancellationToken);
        }

        private static async Task ConnectDisconnectTestAsync(
            WebSocketClient client, 
            string[] events, 
            CancellationToken cancellationToken = default)
        {
            var results = await client.WaitAllEventsAsync<EventArgs>(async () =>
            {
                Console.WriteLine("# Before ConnectAsync");

                Assert.IsFalse(client.IsConnected, nameof(client.IsConnected));

                await client.ConnectAsync(new Uri("ws://echo.websocket.org"), cancellationToken);

                Assert.IsTrue(client.IsConnected, nameof(client.IsConnected));

                Console.WriteLine("# Before SendTextAsync");

                var args = await client.WaitTextAsync(async () =>
                {
                    await client.SendTextAsync("Test", cancellationToken);
                }, cancellationToken);

                Console.WriteLine($"WaitTextAsync: {args.Value}");

                Console.WriteLine("# Before Delay");

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

                Console.WriteLine("# Before DisconnectAsync");

                await client.DisconnectAsync(cancellationToken);

                Console.WriteLine("# After DisconnectAsync");

                Assert.IsFalse(client.IsConnected, nameof(client.IsConnected));
            }, cancellationToken, events);

            Console.WriteLine();
            Console.WriteLine($"WebSocket State: {client.Socket.State}");
            Console.WriteLine($"WebSocket CloseStatus: {client.Socket.CloseStatus}");
            Console.WriteLine($"WebSocket CloseStatusDescription: {client.Socket.CloseStatusDescription}");

            foreach (var pair in results)
            {
                Assert.IsNotNull(pair.Value, $"Client event(\"{pair.Key}\") did not happen");
            }
        }
    }
}
