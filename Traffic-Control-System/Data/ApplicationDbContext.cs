using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using Traffic_Control_System.Models;

namespace Traffic_Control_System.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<PendingUserRequests> PendingUserRequests { get; set; }

        public DbSet<PowerSettings> PowerSettings { get; set; }

        public DbSet<StreamClients> StreamClients { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Rename the ASP.NET Identity tables by removing the "AspNet" prefix
            builder.Entity<ApplicationUser>(entity => entity.ToTable("Users"));
            builder.Entity<IdentityRole>(entity => entity.ToTable("Roles"));
            builder.Entity<IdentityUserRole<string>>(entity => entity.ToTable("UserRoles"));
            builder.Entity<IdentityUserClaim<string>>(entity => entity.ToTable("UserClaims"));
            builder.Entity<IdentityUserLogin<string>>(entity => entity.ToTable("UserLogins"));
            builder.Entity<IdentityRoleClaim<string>>(entity => entity.ToTable("RoleClaims"));
            builder.Entity<IdentityUserToken<string>>(entity => entity.ToTable("UserTokens"));

            builder.Entity<PendingUserRequests>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("PendingUserRequests");
            });

            builder.Entity<PowerSettings>(entity =>
            {
                entity.HasKey(e => e.Key);
            });

            builder.Entity<StreamClients>(entity =>
            {
                entity.HasKey(e => e.UID);
            });
        }
    }
}