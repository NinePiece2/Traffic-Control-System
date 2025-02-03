using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Traffic_Control_System.Hubs
{
    public class ControlHub : Hub
    {
        //// Thread-safe dictionary to map unique identifiers to connection IDs
        //private static readonly ConcurrentDictionary<string, string> _clientConnections = new();

        //// Called when a client connects
        //public override Task OnConnectedAsync()
        //{
        //    Console.WriteLine($"Client connected: {Context.ConnectionId}");
        //    return base.OnConnectedAsync();
        //}

        //// Called when a client disconnects
        //public override Task OnDisconnectedAsync(Exception exception)
        //{
        //    string connectionId = Context.ConnectionId;

        //    // Remove the connection ID from the dictionary
        //    foreach (var pair in _clientConnections)
        //    {
        //        if (pair.Value == connectionId)
        //        {
        //            _clientConnections.TryRemove(pair.Key, out _);
        //            break;
        //        }
        //    }

        //    Console.WriteLine($"Client disconnected: {connectionId}");
        //    return base.OnDisconnectedAsync(exception);
        //}

        //// Method for the client to register with a unique identifier
        //public Task RegisterClient(string clientId)
        //{
        //    string connectionId = Context.ConnectionId;

        //    // Map the unique identifier to the current connection ID
        //    _clientConnections[clientId] = connectionId;

        //    Console.WriteLine($"Registered client '{clientId}' with connection ID '{connectionId}'");
        //    return Task.CompletedTask;
        //}

        // Send a message to a specific client using their unique identifier

        public override Task OnConnectedAsync()
        {
            string connectionId = Context.ConnectionId;
            Clients.Client(connectionId).SendAsync("ReceiveConnectionId", connectionId);

            return base.OnConnectedAsync();
        }

        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }

        public async Task SendMessage(string connectionId)
        {
            //if (_clientConnections.TryGetValue(targetClientId, out string connectionId))
           // {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", "System", "Manual Override");
            //}
            //else
            //{
                //Console.WriteLine($"Client '{targetClientId}' not found.");
            //}
        }

    }
}

