using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Traffic_Control_System.Data;
using System.Collections.Concurrent;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.Data.SqlClient;

namespace Traffic_Control_System.Hubs
{
    public class ControlHub : Hub
    {

        private readonly ApplicationDbContext _context;
        private readonly string _connectionString;

        public ControlHub(ApplicationDbContext context)
        {
            _context = context;
            _connectionString = _context.Database.GetDbConnection().ConnectionString;
        }

        public override Task OnConnectedAsync()
        {
            string connectionId = Context.ConnectionId;
            Clients.Client(connectionId).SendAsync("ReceiveConnectionId", connectionId);

            string clientType = Context.GetHttpContext()?.Request.Query["clientType"];

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
                    MERGE INTO SignalRClient AS target
                    USING (SELECT @ClientType AS ClientType) AS source
                    ON target.ClientType = source.ClientType
                    WHEN MATCHED THEN
                        UPDATE SET target.ConnectionID = @ConnectionID, target.LastUpdated = GETDATE()
                    WHEN NOT MATCHED THEN
                        INSERT (ConnectionID, ClientType, LastUpdated) VALUES (@ConnectionID, @ClientType, GETDATE());", connection))
                {
                    command.Parameters.AddWithValue("@ConnectionID", connectionId);
                    command.Parameters.AddWithValue("@ClientType", clientType ?? "Unknown");
                    command.ExecuteNonQuery(); // Synchronous execution
                }
            }
            
            _context.

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            string connectionId = Context.ConnectionId;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("DELETE FROM SignalRClient WHERE ConnectionID = @ConnectionID", connection))
                {
                    command.Parameters.AddWithValue("@ConnectionID", connectionId);
                    command.ExecuteNonQuery(); // Synchronous execution
                }
            }

            return base.OnDisconnectedAsync(exception);
        }


        //public override Task OnConnectedAsync()
        //{
        //    string connectionId = Context.ConnectionId;
        //    Clients.Client(connectionId).SendAsync("ReceiveConnectionId", connectionId);

        //    return base.OnConnectedAsync();
        //}

        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }

        public async Task SendMessage(string connectionId)
        {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", "System", "Manual Override");
        }
            
        public async Task BroadcastMessage(string user , string message)
        {
            await Clients.All.SendAsync("ReceiveMessage","System", "Manual Override");
        }
    }

}


