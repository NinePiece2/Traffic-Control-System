using dotenv.net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Traffic_Control_System.Data;
using Traffic_Control_System.Models;
using Traffic_Control_System.Services;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System.Runtime.InteropServices;
using Traffic_Control_System.Migrations;
using Microsoft.AspNetCore.WebSockets;

namespace Traffic_Control_System
{
    public class Program
    {
        public static async Task Main(string[] args)
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

            builder.Services.AddWebSockets(options => {
                options.KeepAliveInterval = TimeSpan.FromSeconds(120);
            });

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
            // For direct file access
            string hlsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "hls");
            Directory.CreateDirectory(hlsDirectory);

            var contentTypeProvider = new FileExtensionContentTypeProvider();
            contentTypeProvider.Mappings[".ism"] = "application/vnd.ms-sstr+xml";
            contentTypeProvider.Mappings[".m3u8"] = "application/vnd.apple.mpegurl";
            contentTypeProvider.Mappings[".ts"] = "video/MP2T";

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "hls")),
                RequestPath = "/hls",
                ContentTypeProvider = contentTypeProvider,
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "https://localhost:44328");
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
                }
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            // Enable WebSocket middleware
            app.UseWebSockets();


            var ffmpegBinFilename = "";

            Directory.CreateDirectory("ffmpegBins");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string ffmpegDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ffmpegBins", "ffmpeg.exe");
                await DownloadFileAsync("https://cdn.romitsagu.com/files/FFmpeg/ffmpeg.exe", ffmpegDirectory);

                ffmpegBinFilename = "ffmpeg.exe";
            }
            else
            {
                string ffmpegDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ffmpegBins", "ffmpeg");
                await DownloadFileAsync("https://cdn.romitsagu.com/files/FFmpeg/ffmpeg", ffmpegDirectory);

                ffmpegBinFilename = "ffmpeg";
            }

            app.Map("/ws", async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    Console.WriteLine("WebSocket connection established.");

                    string hlsPath = Path.Combine(hlsDirectory, "stream.m3u8");

                    using (var ffmpeg = new Process())
                    {
                        string ffmpegFileDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ffmpegBins", ffmpegBinFilename);

                        ffmpeg.StartInfo.FileName = ffmpegFileDirectory;
                        ffmpeg.StartInfo.Arguments = $"-f rawvideo -pixel_format bgr24 -video_size 640x480 -i - -c:v libx264 -preset ultrafast -tune zerolatency -pix_fmt yuv420p -f hls -hls_time 0.2 -hls_list_size 3 -hls_flags delete_segments {hlsPath}";

                        ffmpeg.StartInfo.UseShellExecute = false;
                        ffmpeg.StartInfo.RedirectStandardInput = true;
                        ffmpeg.StartInfo.RedirectStandardError = true; // Redirect standard error
                        ffmpeg.Start();

                        string errorOutput = string.Empty;
                        ffmpeg.ErrorDataReceived += (sender, e) =>
                        {
                            if (e.Data != null)
                            {
                                errorOutput += e.Data + Environment.NewLine; // Accumulate error output
                            }
                        };
                        ffmpeg.BeginErrorReadLine(); // Begin reading error output

                        try
                        {
                            byte[] buffer = new byte[1024 * 32];
                            while (webSocket.State == System.Net.WebSockets.WebSocketState.Open)
                            {
                                var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                                if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                                {
                                    await webSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                                    break;
                                }

                                if (ffmpeg.HasExited)
                                {
                                    Console.WriteLine("FFmpeg process has exited unexpectedly.");
                                    break;
                                }

                                try
                                {
                                    await ffmpeg.StandardInput.BaseStream.WriteAsync(buffer, 0, result.Count);
                                }
                                catch (IOException ioEx)
                                {
                                    Console.WriteLine("Stream write error: " + ioEx.Message);
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error during WebSocket processing: " + ex.Message);
                        }
                        finally
                        {
                            try
                            {
                                ffmpeg.StandardInput.Close();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error closing ffmpeg input: " + ex.Message);
                            }

                            if (!ffmpeg.HasExited)
                            {
                                ffmpeg.WaitForExit();
                            }
                            Console.WriteLine($"FFmpeg exited with code {ffmpeg.ExitCode}. Error Output: {errorOutput}");
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


        private static async Task DownloadFileAsync(string fileUrl, string destinationPath)
        {
            // Check if the file already exists
            if (File.Exists(destinationPath))
            {
                Console.WriteLine($"File already exists at {destinationPath}. Download skipped.");
                return; // Exit the method if the file already exists
            }

            using (HttpClient httpClient = new HttpClient())
            {
                using (HttpResponseMessage response = await httpClient.GetAsync(fileUrl))
                {
                    response.EnsureSuccessStatusCode(); // Throw if not a success code.

                    using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }

                    Console.WriteLine($"File downloaded successfully to {destinationPath}.");
                }
            }
        }
    }
}
