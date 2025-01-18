using Microsoft.AspNetCore.SignalR;

namespace Traffic_Control_System.Hubs
{
    public class ControlHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
