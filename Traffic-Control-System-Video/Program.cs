using LiveStreamingServerNet;
using LiveStreamingServerNet.StreamProcessor.AspNetCore.Configurations;
using LiveStreamingServerNet.StreamProcessor.AspNetCore.Installer;
using LiveStreamingServerNet.StreamProcessor.Contracts;
using LiveStreamingServerNet.StreamProcessor.Hls.Configurations;
using LiveStreamingServerNet.StreamProcessor.Installer;
using LiveStreamingServerNet.StreamProcessor.Utilities;
using LiveStreamingServerNet.Utilities.Contracts;
using System.Net;
using System.Runtime.InteropServices;
using Traffic_Control_System_Video.Handlers;
using LiveStreamingServerNet.Flv;
using LiveStreamingServerNet.Flv.Installer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;
using Traffic_Control_System.Data;
using LiveStreamingServerNet.Rtmp.Server.Auth.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Traffic_Control_System_Video.Services;

namespace Traffic_Control_System_Video
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("TrafficControlSystemContextConnection");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddScoped<AuthorizationHandler>();


            // Add services to the container.
            builder.Services.AddLiveStreamingServer().AddLogging();

            builder.Services.AddCors(options =>
                options.AddDefaultPolicy(policy =>
                    policy.AllowAnyHeader()
                          .AllowAnyOrigin()
                          .AllowAnyMethod()
                )
            );

            builder.Services.AddControllers();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                };
            });

            builder.Services.AddSingleton<ITokenService, TokenService>();

            var app = builder.Build();

            app.UseCors();

            app.UseHttpFlv();

            app.MapControllers();

            app.UseAuthentication();

            app.UseAuthorization();

            app.Run();
        }

        private static IServiceCollection AddLiveStreamingServer(this IServiceCollection services)
        {
            var scope = services.BuildServiceProvider().CreateScope();

            var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "hls");
            new DirectoryInfo(outputDir).Create();

            return services.AddLiveStreamingServer(
                new IPEndPoint(IPAddress.Any, 1935),
                options => options
                    .Configure(options => options.EnableGopCaching = false)
                    .AddFlv()
                    .AddAuthorizationHandler<AuthorizationHandler>()
                    .AddStreamProcessor(options =>
                    {
                        options.AddStreamProcessorEventHandler(svc =>
                                new StreamProcessorEventListener(outputDir, svc.GetRequiredService<ILogger<StreamProcessorEventListener>>()));
                    })

                    //.AddAdaptiveHlsTranscoder(options =>
                    //{
                    //    // Check Platform
                    //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    //    {
                    //        var ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "ffmpegBins", "ffmpeg.exe");
                    //        var ffprobePath = Path.Combine(Directory.GetCurrentDirectory(), "ffmpegBins", "ffprobe.exe");
                    //        options.FFmpegPath = ffmpegPath;
                    //        options.FFprobePath = ffprobePath;
                    //    }
                    //    else
                    //    {
                    //        //options.FFmpegPath = ExecutableFinder.FindExecutableFromPATH("ffmpeg")!;
                    //        //options.FFprobePath = ExecutableFinder.FindExecutableFromPATH("ffprobe")!;
                    //        var ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "ffmpegBins", "ffmpeg");
                    //        var ffprobePath = Path.Combine(Directory.GetCurrentDirectory(), "ffmpegBins", "ffprobe");
                    //        options.FFmpegPath = ffmpegPath;
                    //        options.FFprobePath = ffprobePath;
                    //    }

                    //    options.OutputPathResolver = new HlsOutputPathResolver(outputDir);
                    //    options.PerformanceOptions = new PerformanceOptions(4, 4096);
                    //    options.CleanupDelay = TimeSpan.FromSeconds(10.0);
                    //    options.DownsamplingFilters =
                    //    [
                    //        new DownsamplingFilter(
                    //            Name: "360p",
                    //            Height: 360,
                    //            MaxVideoBitrate: "600k",
                    //            MaxAudioBitrate: "64k"
                    //        ),

                    //        new DownsamplingFilter(
                    //            Name: "480p",
                    //            Height: 480,
                    //            MaxVideoBitrate: "1500k",
                    //            MaxAudioBitrate: "128k"
                    //        ),

                    //        new DownsamplingFilter(
                    //            Name: "720p",
                    //            Height: 720,
                    //            MaxVideoBitrate: "3000k",
                    //            MaxAudioBitrate: "256k"
                    //        )
                    //    ];

                    //    // Hardware acceleration 
                    //    // options.VideoDecodingArguments = "-hwaccel auto -c:v h264_cuvid";
                    //    // options.VideoEncodingArguments = "-c:v h264_nvenc -g 30";
                    //})
                    .AddHlsTransmuxer(options =>
                    {
                        options.OutputPathResolver = new HlsOutputPathResolver(outputDir);
                        options.SegmentListSize = 5;
                        options.CleanupDelay = TimeSpan.FromSeconds(10.0);
                        options.MinSegmentLength = TimeSpan.FromSeconds(0.5);
                    })
            );
        }
    }
}
