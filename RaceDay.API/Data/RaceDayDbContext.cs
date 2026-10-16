using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------- USERS ----------
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();

            e.ToTable(t => t.HasCheckConstraint(
                "CK_Users_Role",
                "[Role] IN ('Organiser','Participant')"));
        });

        // ---------- USER PROFILE (1:1 with Users) ----------
        modelBuilder.Entity<UserProfile>(e =>
        {
            e.HasIndex(p => p.UserID).IsUnique();

            e.HasOne(p => p.User)
             .WithOne(u => u.UserProfile)
             .HasForeignKey<UserProfile>(p => p.UserID)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- EVENTS ----------
        modelBuilder.Entity<Event>(e =>
        {
            e.Property(x => x.Distance).HasColumnType("decimal(5,2)");

            e.ToTable(t => t.HasCheckConstraint(
                "CK_Events_EventType",
                "[EventType] IN ('Run','Walk','Cycle')"));

            e.HasOne(x => x.Organiser)
             .WithMany(u => u.OrganisedEvents)
             .HasForeignKey(x => x.OrganiserID)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- CATEGORIES ----------
        modelBuilder.Entity<Category>(e =>
        {
            e.HasIndex(c => new { c.EventID, c.CategoryName }).IsUnique();

            e.HasOne(c => c.Event)
             .WithMany(ev => ev.Categories)
             .HasForeignKey(c => c.EventID)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- ENROLLMENTS ----------
        modelBuilder.Entity<Enrollment>(e =>
        {
            e.HasIndex(en => new { en.UserID, en.EventID }).IsUnique();

            e.ToTable(t => t.HasCheckConstraint(
                "CK_Enrollments_Status",
                "[Status] IN ('Confirmed','Cancelled')"));

            e.HasOne(en => en.User)
             .WithMany(u => u.Enrollments)
             .HasForeignKey(en => en.UserID)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(en => en.Event)
             .WithMany(ev => ev.Enrollments)
             .HasForeignKey(en => en.EventID)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(en => en.Category)
             .WithMany(c => c.Enrollments)
             .HasForeignKey(en => en.CategoryID)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- RESULTS ----------
        modelBuilder.Entity<Result>(e =>
        {
            e.HasIndex(r => r.EnrollmentID).IsUnique();

            e.HasOne(r => r.Enrollment)
             .WithOne(en => en.Result)
             .HasForeignKey<Result>(r => r.EnrollmentID)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}