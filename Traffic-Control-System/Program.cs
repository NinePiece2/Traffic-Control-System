using dotenv.net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Traffic_Control_System.Data;
using Traffic_Control_System.Models;
using Traffic_Control_System.Services;
using Traffic_Control_System.Hubs;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

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

            builder.Services.AddDataProtection()
                .PersistKeysToDbContext<ApplicationDbContext>();

            builder.Services.AddControllersWithViews();
            builder.Services.AddSignalR();

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
            builder.Services.AddTransient<IVideoService, VideoService>();

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
            app.MapHub<ControlHub>("/controlhub");

            app.Run();
        }
        
    }
}
