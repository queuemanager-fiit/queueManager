using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }
    public ApplicationDbContext() { }
    public DbSet<User> Users { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<EventCategory> EventCategories { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=158.160.81.218;Port=5432;Database=queuemanager_db;Username=real;Password=real");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Group>(entity =>
        {

            entity.Property(g => g.CategoriesIds)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions)null) ?? new List<Guid>()
                )
                .HasColumnType("jsonb");

            entity.Property(g => g.EventsIds)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions)null) ?? new List<Guid>()
                )
                .HasColumnType("jsonb");

            entity.Property(g => g.UsersTelegramIds)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<long>>(v, (JsonSerializerOptions)null) ?? new List<long>()
                )
                .HasColumnType("jsonb");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.TelegramId);
            entity.Property(u => u.FullName).IsRequired();
            entity.Property(u => u.Username);
            entity.Property(u => u.IsAdmin).HasDefaultValue(false);
            entity.Property(u => u.AveragePosition).HasDefaultValue(0.0);
            entity.Property(u => u.ParticipationCount).HasDefaultValue(0);

            entity.Property(u => u.GroupCodes)
                .HasConversion(
                    v => string.Join(';', v),
                    v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
                )
                .HasColumnType("text");
        });

        modelBuilder.Entity<EventCategory>(entity =>
        {
            entity.HasKey(ec => ec.Id);
            entity.Property(ec => ec.SubjectName).IsRequired();
            entity.Property(ec => ec.IsAutoCreate).HasDefaultValue(false);
            entity.Property(ec => ec.GroupCode).HasMaxLength(20).IsRequired();

            entity.Property(ec => ec.UnfinishedUsersTelegramIds)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<long>>(v, (JsonSerializerOptions)null) ?? new List<long>()
                )
                .HasColumnType("jsonb");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OccurredOn);
            entity.Property(e => e.FormationTime);
            entity.Property(e => e.DeletionTime);
            entity.Property(e => e.NotificationTime);
            entity.Property(e => e.IsNotified).HasDefaultValue(false);
            entity.Property(e => e.IsFormed).HasDefaultValue(false);
            entity.Property(e => e.GroupCode).HasMaxLength(20).IsRequired();

            entity.Property(e => e.ParticipantsTelegramIds)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<long>>(v, (JsonSerializerOptions)null) ?? new List<long>()
                )
                .HasColumnType("jsonb");

            entity.Property(e => e.Preferences)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<UserPreference>>(v, (JsonSerializerOptions)null) ?? new List<UserPreference>()
                )
                .HasColumnType("jsonb");
        });

        base.OnModelCreating(modelBuilder);
    }
}
