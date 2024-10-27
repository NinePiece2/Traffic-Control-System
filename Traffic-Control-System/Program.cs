using dotenv.net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Traffic_Control_System.Data;
using Traffic_Control_System.Models;
using Traffic_Control_System.Services;

namespace Traffic_Control_System
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            if (builder.Environment.IsDevelopment())
            {
                DotEnv.Load();
            }
            else
            {
                builder.Configuration.AddEnvironmentVariables();
            }

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("TrafficControlSystemContextConnection");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+/ ";
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddDefaultUI(); 

            builder.Services.AddControllersWithViews();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "TrafficControlSystem";
                options.LoginPath = "/Identity/Account/Login";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;
            });

            // Syncfusion License Registration
            var syncfusionLicense = Environment.GetEnvironmentVariable("SyncfusionLicense");
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionLicense);

            builder.Services.AddTransient<IEmailService, EmailService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            // Enable WebSocket middleware
            app.UseWebSockets();

            app.Map("/ws", async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    Console.WriteLine("WebSocket connection established.");

                    byte[] buffer = new byte[1024 * 4];

                    while (webSocket.State == System.Net.WebSockets.WebSocketState.Open)
                    {
                        var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                        if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                        {
                            await webSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        }
                        else
                        {
                            // Decode received data
                            var base64Data = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                            byte[] imageData = Convert.FromBase64String(base64Data);

                            // Optional: Process or save the image data
                            Console.WriteLine($"Received frame with {imageData.Length} bytes");
                        }
                    }
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            });

            app.Run();
        }
    }
}
