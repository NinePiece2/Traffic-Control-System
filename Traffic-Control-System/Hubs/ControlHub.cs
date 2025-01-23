using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Traffic_Control_System.Hubs
{
    public class ControlHub : Hub
    {

        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            await Clients.Client(connectionId).SendAsync("ReceiveConnectionId", connectionId);
            await base.OnConnectedAsync();
        }

        public async Task SendMessage(string connectionId, string user, string message)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveMessage", user, message);
        }
    }
}
