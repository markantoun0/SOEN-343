﻿﻿using Microsoft.EntityFrameworkCore;
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
        public DbSet<MobilityLocation> MobilityLocations { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
    }
}