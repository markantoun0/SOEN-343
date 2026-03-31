using Microsoft.EntityFrameworkCore;
using SUMMS.Api.Domain.Models;
using YourProject.Models;

namespace SUMMS.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<MobilityLocation> MobilityLocations { get; set; }
        public DbSet<Reservation> Reservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Admin>()
                .HasIndex(a => a.Email)
                .IsUnique();

            modelBuilder.Entity<MobilityLocation>()
                .HasIndex(m => m.PlaceId)
                .IsUnique();

            modelBuilder.Entity<Reservation>()
                .Property(r => r.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => new { r.MobilityLocationId, r.Status, r.IsDeleted });

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        }
    }
}