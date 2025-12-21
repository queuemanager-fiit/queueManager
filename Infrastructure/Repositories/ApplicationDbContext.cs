using Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Group>(entity =>
        {

            entity.HasMany(g => g.Users)
                .WithOne(u => u.Group)
                .HasForeignKey(u => u.GroupCode)
                .HasPrincipalKey(g => g.Code)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasMany(g => g.Events)
                .WithOne()
                .HasForeignKey(e => e.GroupCode)
                .HasPrincipalKey(g => g.Code)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasMany(g => g.Categories)
                .WithOne()
                .HasForeignKey(ec => ec.GroupCode)
                .HasPrincipalKey(g => g.Code)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.TelegramId);
            entity.Property(u => u.GroupCode).HasMaxLength(20);
            
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

            entity.HasOne<Group>()
                .WithMany(g => g.Categories)
                .HasForeignKey(ec => ec.GroupCode)
                .HasPrincipalKey(g => g.Code)
                .OnDelete(DeleteBehavior.Cascade);
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

            entity.HasOne(typeof(EventCategory), "Category")
                .WithMany()
                .HasForeignKey("CategoryId")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(typeof(User), "Participants")
                .WithMany()
                .UsingEntity(j => j.ToTable("EventParticipants"));
            
            entity.HasOne<Group>()
                .WithMany(g => g.Events)
                .HasForeignKey(e => e.GroupCode)
                .HasPrincipalKey(g => g.Code)
                .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}