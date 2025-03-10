using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Traffic_Control_System.Data;
using Traffic_Control_System.Models;

namespace Traffic_Control_System.Hubs
{
    public class ControlHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ControlHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            string connectionId = Context.ConnectionId;
            await Clients.Client(connectionId).SendAsync("ReceiveConnectionId", connectionId);

            string clientType = Context.GetHttpContext()?.Request.Query["clientType"] ?? "Unknown";
            int? activeSignalId = null;

            if (clientType == "JavaScript")
            {
                string activeSignalIdString = Context.GetHttpContext()?.Request.Query["activeSignalId"];
                if (!string.IsNullOrEmpty(activeSignalIdString) && int.TryParse(activeSignalIdString, out int parsedUid))
                {
                    activeSignalId = parsedUid;
                }
            } else if(clientType == "Python"){
                string deviceIdString = Context.GetHttpContext()?.Request.Query["deviceId"];

                if (deviceIdString != null){
                    deviceIdString = deviceIdString.Trim();
                }

                activeSignalId = _context.ActiveSignals
                    .Where(a => a.DeviceStreamUID == _context.StreamClients
                        .Where(c => c.DeviceStreamID == deviceIdString)
                        .Select(c => c.UID)
                        .FirstOrDefault())
                    .Select(a => a.ID)
                    .FirstOrDefault();
            }
            
            
            var newClient = new SignalRClient
            {
                ActiveSignalID = activeSignalId,
                ConnectionID = connectionId,
                ClientType = clientType,
                LastUpdated = DateTime.UtcNow
            };

            _context.SignalRClient.Add(newClient);
            await _context.SaveChangesAsync();

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string connectionId = Context.ConnectionId;
            var client = await _context.SignalRClient.FirstOrDefaultAsync(c => c.ConnectionID == connectionId);
            if (client != null)
            {
                _context.SignalRClient.Remove(client);
                await _context.SaveChangesAsync();
            }

            await base.OnDisconnectedAsync(exception);
        }

        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }

        public async Task SendMessageToClientByDeviceID(string deviceID, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                Console.WriteLine("SendMessageToClientBySignalID failed: message is null or empty.");
                return;
            }

            var activeSignalId = _context.ActiveSignals
                    .Where(a => a.DeviceStreamUID == _context.StreamClients
                        .Where(c => c.DeviceStreamID == deviceID)
                        .Select(c => c.UID)
                        .FirstOrDefault())
                    .Select(a => a.ID)
                    .FirstOrDefault();

            var signalRClients = await _context.SignalRClient.Where(c => c.ActiveSignalID == activeSignalId && c.ClientType == "JavaScript").ToListAsync();
            if (signalRClients.Any())
            {
                foreach (var client in signalRClients)
                {
                    Console.WriteLine($"Sending message to {client.ClientType} client with connection ID {client.ConnectionID}.");
                    await Clients.Client(client.ConnectionID).SendAsync("ReceiveMessage", "System", message);
                }
            }
            else
            {
                Console.WriteLine($"No active clients found for UID {activeSignalId}.");
            }

        }

        public async Task SendMessageToClientFromJS(string activeSignalId, string message)
        {
            //Console.WriteLine($"Sending message to JavaScript client with activeSignalId {activeSignalId}.");

            if (!int.TryParse(activeSignalId, out int activeSignalIdInt))
            {
                Console.WriteLine("SendMessageToClientFromJS failed: activeSignalId is null or invalid.");
                return;
            }

            if (activeSignalIdInt == 0)
            {
                Console.WriteLine("SendMessageToClientBySignalID failed: activeSignalId is null or invalid.");
                return;
            }

            if (string.IsNullOrEmpty(message))
            {
                Console.WriteLine("SendMessageToClientBySignalID failed: message is null or empty.");
                return;
            }

            try{
                var signalRClientsCount = _context.SignalRClient
                                        .Where(c => c.ActiveSignalID == activeSignalIdInt && c.ClientType == "Python")
                                        .OrderByDescending(c => c.LastUpdated)
                                        .Count();
                if (signalRClientsCount > 0){
                    var signalRClients = _context.SignalRClient
                                            .Where(c => c.ActiveSignalID == activeSignalIdInt && c.ClientType == "Python")
                                            .OrderByDescending(c => c.LastUpdated)
                                            .FirstOrDefault();
                    await Clients.Client(signalRClients.ConnectionID).SendAsync("ReceiveMessage", "System", message);
                }
            }
            catch (Exception e){
                Console.WriteLine($"Error: {e.Message}");
            }
        }

        public async Task SendMessage(string connectionId)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveMessage", "System", "Manual Override");
        }

        public async Task BroadcastMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
