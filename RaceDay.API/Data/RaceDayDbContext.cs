
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(
        DbContextOptions<RaceDayDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ROLES
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles", table =>
                table.HasCheckConstraint(
                    "CK_Roles_RoleName",
                    "[RoleName] IN ('Organiser', 'Participant')"));

            entity.HasKey(r => r.RoleId);

            entity.Property(r => r.RoleName)
                .HasColumnType("varchar(30)")
                .IsRequired();

            entity.HasIndex(r => r.RoleName).IsUnique();
        });

        // USERS
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(u => u.UserId);

            entity.Property(u => u.FirstName)
                .HasColumnType("varchar(80)")
                .IsRequired();

            entity.Property(u => u.LastName)
                .HasColumnType("varchar(80)")
                .IsRequired();

            entity.Property(u => u.Email)
                .HasColumnType("varchar(255)")
                .IsRequired();

            entity.Property(u => u.PasswordHash)
                .HasColumnType("varchar(255)")
                .IsRequired();

            entity.Property(u => u.PhoneNumber)
                .HasColumnType("varchar(30)");

            entity.Property(u => u.DateOfBirth)
                .HasColumnType("date");

            entity.Property(u => u.ProfilePictureUrl)
                .HasColumnType("varchar(1000)");

            entity.Property(u => u.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // EVENTS
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events", table =>
            {
                table.HasCheckConstraint(
                    "CK_Events_DistanceKm",
                    "[DistanceKm] > 0");

                table.HasCheckConstraint(
                    "CK_Events_EventType",
                    "[EventType] IN ('Run', 'Walk', 'Cycle')");
            });

            entity.HasKey(e => e.EventId);

            entity.Property(e => e.EventName)
                .HasColumnType("varchar(150)")
                .IsRequired();

            entity.Property(e => e.Description)
                .HasColumnType("varchar(max)");

            entity.Property(e => e.EventDate)
                .HasColumnType("date");

            entity.Property(e => e.Location)
                .HasColumnType("varchar(200)")
                .IsRequired();

            entity.Property(e => e.DistanceKm)
                .HasColumnType("decimal(6,2)");

            entity.Property(e => e.EventType)
                .HasColumnType("varchar(20)")
                .IsRequired();

            entity.Property(e => e.BannerImageUrl)
                .HasColumnType("varchar(1000)");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Organiser)
                .WithMany(u => u.OrganisedEvents)
                .HasForeignKey(e => e.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CATEGORIES
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories", table =>
            {
                table.HasCheckConstraint(
                    "CK_Categories_AgeRange",
                    "[MinAge] >= 0 AND [MaxAge] >= [MinAge]");

                table.HasCheckConstraint(
                    "CK_Categories_DistanceKm",
                    "[DistanceKm] > 0");
            });

            entity.HasKey(c => c.CategoryId);

            entity.Property(c => c.CategoryName)
                .HasColumnType("varchar(100)")
                .IsRequired();

            entity.Property(c => c.DistanceKm)
                .HasColumnType("decimal(6,2)");

            entity.HasIndex(c => new { c.EventId, c.CategoryName })
                .IsUnique();

            entity.HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ENROLMENTS
        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.ToTable("Enrolments", table =>
                table.HasCheckConstraint(
                    "CK_Enrolments_Status",
                    "[EnrolmentStatus] IN ('Pending', 'Confirmed', 'Cancelled')"));

            entity.HasKey(e => e.EnrolmentId);

            entity.Property(e => e.EnrolmentDate)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.EnrolmentStatus)
                .HasColumnType("varchar(20)")
                .HasDefaultValue("Confirmed")
                .IsRequired();

            entity.HasIndex(e => new { e.ParticipantId, e.EventId })
                .IsUnique();

            entity.HasOne(e => e.Participant)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(e => e.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Event)
                .WithMany(ev => ev.Enrolments)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // RESULTS
        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Results", table =>
                table.HasCheckConstraint(
                    "CK_Results_FinishingPosition",
                    "[FinishingPosition] > 0"));

            entity.HasKey(r => r.ResultId);

            entity.Property(r => r.FinishTime)
                .HasColumnType("time");

            entity.Property(r => r.PublishedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(r => r.EnrolmentId).IsUnique();

            entity.HasOne(r => r.Enrolment)
                .WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
