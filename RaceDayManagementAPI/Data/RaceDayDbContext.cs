using Microsoft.EntityFrameworkCore;
using RaceDayManagementAPI.Models;

namespace RaceDayManagementAPI.Data
{
    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<RaceRoute> Routes => Set<RaceRoute>();
        public DbSet<Enrolment> Enrolments => Set<Enrolment>();
        public DbSet<Result> Results => Set<Result>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            // ---------------- USERS ----------------
            mb.Entity<User>(e =>
            {
                e.HasKey(u => u.UserID);
                e.Property(u => u.UserID).ValueGeneratedOnAdd();
                e.Property(u => u.FullName).IsRequired();
                e.Property(u => u.Email).IsRequired();
                e.Property(u => u.PasswordHash).IsRequired();
                e.Property(u => u.Role).IsRequired();

                e.HasIndex(u => u.Email).IsUnique();
            });

            // ---------------- EVENTS ----------------
            mb.Entity<Event>(e =>
            {
                e.HasKey(x => x.EventID);
                e.Property(x => x.EventID).ValueGeneratedOnAdd();
                e.Property(x => x.EventName).IsRequired();
                e.Property(x => x.EventDate).IsRequired();
                e.Property(x => x.Location).IsRequired();
                e.Property(x => x.EventType).IsRequired();

                e.HasOne(x => x.Organiser)
                 .WithMany(u => u.OrganisedEvents)
                 .HasForeignKey(x => x.OrganiserID)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------------- CATEGORIES ----------------
            mb.Entity<Category>(e =>
            {
                e.HasKey(c => c.CategoryID);
                e.Property(c => c.CategoryID).ValueGeneratedOnAdd();
                e.Property(c => c.CategoryName).IsRequired();

                e.HasOne(c => c.Event)
                 .WithMany(ev => ev.Categories)
                 .HasForeignKey(c => c.EventID)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------------- ROUTES ----------------
            mb.Entity<RaceRoute>(e =>
            {
                e.HasKey(r => r.RouteID);
                e.Property(r => r.RouteID).ValueGeneratedOnAdd();
                e.Property(r => r.RouteName).IsRequired();

                e.HasOne(r => r.Event)
                 .WithMany(ev => ev.Routes)
                 .HasForeignKey(r => r.EventID)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------------- ENROLMENTS ----------------
            mb.Entity<Enrolment>(e =>
            {
                e.HasKey(x => x.EnrolmentID);
                e.Property(x => x.EnrolmentID).ValueGeneratedOnAdd();
                e.Property(x => x.Status).IsRequired();

                e.HasIndex(x => new { x.ParticipantID, x.CategoryID }).IsUnique();

                e.HasOne(x => x.Participant)
                 .WithMany(u => u.Enrolments)
                 .HasForeignKey(x => x.ParticipantID)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Category)
                 .WithMany(c => c.Enrolments)
                 .HasForeignKey(x => x.CategoryID)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------------- RESULTS ----------------
            mb.Entity<Result>(e =>
            {
                e.HasKey(r => r.ResultID);
                e.Property(r => r.ResultID).ValueGeneratedOnAdd();
                e.Property(r => r.Status).IsRequired();

                e.HasIndex(r => r.EnrolmentID).IsUnique();

                e.HasOne(r => r.Enrolment)
                 .WithOne(en => en.Result)
                 .HasForeignKey<Result>(r => r.EnrolmentID)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            SeedData(mb);
        }

        private static void SeedData(ModelBuilder mb)
        {
            var seedDate = new DateTime(2026, 1, 1, 8, 0, 0);
            const string hash = "$2a$11$0lJPQ4Do58snKUGFEwc.eOq9.9YnRF0h4IgjFw35sfxWIjMUWlbY6";

            mb.Entity<User>().HasData(
                new User { UserID = 1, FullName = "Thabo Mokoena", Email = "thabo.mokoena@raceday.co.za", PasswordHash = hash, Role = "Organiser", CreatedAt = seedDate },
                new User { UserID = 2, FullName = "Lindiwe Dube", Email = "lindiwe.dube@raceday.co.za", PasswordHash = hash, Role = "Organiser", CreatedAt = seedDate },
                new User { UserID = 3, FullName = "Sarah van Wyk", Email = "sarah.vanwyk@raceday.co.za", PasswordHash = hash, Role = "Participant", CreatedAt = seedDate },
                new User { UserID = 4, FullName = "Musa Nkosi", Email = "musa.nkosi@raceday.co.za", PasswordHash = hash, Role = "Participant", CreatedAt = seedDate }
            );

            mb.Entity<Event>().HasData(
                new Event { EventID = 1, OrganiserID = 1, EventName = "Benoni Park Run Challenge", Description = "A community road running event through Benoni Lake precinct.", EventDate = new DateTime(2026, 10, 10), Location = "Benoni, Gauteng", DistanceKm = 10.0m, EventType = "Run", CreatedAt = seedDate },
                new Event { EventID = 2, OrganiserID = 1, EventName = "Gauteng Cycle Classic", Description = "An annual charity cycling event in Pretoria.", EventDate = new DateTime(2026, 11, 14), Location = "Pretoria, Gauteng", DistanceKm = 80.0m, EventType = "Cycle", CreatedAt = seedDate },
                new Event { EventID = 3, OrganiserID = 2, EventName = "Durban Coastal Marathon", Description = "A scenic marathon along the Durban coastline.", EventDate = new DateTime(2026, 9, 20), Location = "Durban, KwaZulu-Natal", DistanceKm = 42.2m, EventType = "Run", CreatedAt = seedDate }
            );

            mb.Entity<Category>().HasData(
                new Category { CategoryID = 1, EventID = 1, CategoryName = "5km Fun Run", DistanceKm = 5.0m, EntryFee = 80.00m },
                new Category { CategoryID = 2, EventID = 1, CategoryName = "10km Race", DistanceKm = 10.0m, EntryFee = 120.00m },
                new Category { CategoryID = 3, EventID = 2, CategoryName = "40km Cycle", DistanceKm = 40.0m, EntryFee = 250.00m },
                new Category { CategoryID = 4, EventID = 2, CategoryName = "80km Cycle", DistanceKm = 80.0m, EntryFee = 350.00m },
                new Category { CategoryID = 5, EventID = 3, CategoryName = "Half Marathon", DistanceKm = 21.1m, EntryFee = 200.00m },
                new Category { CategoryID = 6, EventID = 3, CategoryName = "Full Marathon", DistanceKm = 42.2m, EntryFee = 300.00m }
            );

            mb.Entity<RaceRoute>().HasData(
                new RaceRoute { RouteID = 1, EventID = 1, RouteName = "Benoni Lake Loop", DistanceKm = 10.0m, MapFileUrl = "/maps/benoni-lake-loop.png" },
                new RaceRoute { RouteID = 2, EventID = 2, RouteName = "Pretoria Cycle Route", DistanceKm = 80.0m, MapFileUrl = "/maps/pretoria-cycle-route.png" },
                new RaceRoute { RouteID = 3, EventID = 3, RouteName = "Durban Coastal Route", DistanceKm = 42.2m, MapFileUrl = "/maps/durban-coastal-route.png" }
            );

            mb.Entity<Enrolment>().HasData(
                new Enrolment { EnrolmentID = 1, ParticipantID = 3, CategoryID = 2, EnrolmentDate = new DateTime(2026, 8, 1), Status = "Active" },
                new Enrolment { EnrolmentID = 2, ParticipantID = 4, CategoryID = 4, EnrolmentDate = new DateTime(2026, 8, 5), Status = "Active" },
                new Enrolment { EnrolmentID = 3, ParticipantID = 3, CategoryID = 5, EnrolmentDate = new DateTime(2026, 8, 10), Status = "Active" }
            );

            mb.Entity<Result>().HasData(
                new Result { ResultID = 1, EnrolmentID = 1, FinishTime = new TimeSpan(0, 52, 30), Position = 14, Status = "Finished" },
                new Result { ResultID = 2, EnrolmentID = 2, FinishTime = new TimeSpan(2, 45, 10), Position = 8, Status = "Finished" }
            );
        }
    }
}