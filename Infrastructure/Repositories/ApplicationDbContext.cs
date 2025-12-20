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

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OccurredOn);
            entity.Property(e => e.FormationTime);
            entity.Property(e => e.DeletionTime);
            entity.Property(e => e.NotificationTime);
            entity.Property(e => e.IsNotified);
            entity.Property(e => e.IsFormed);
            entity.Property(e => e.GroupCode);

            entity.HasOne(typeof(EventCategory), "Category")
                .WithMany()
                .HasForeignKey("CategoryId");

            entity.HasMany(typeof(User), "Participants")
                .WithMany()
                .UsingEntity(j => j.ToTable("EventParticipants"));
        });


        modelBuilder.Entity<EventCategory>(entity =>
        {
            entity.HasKey(ec => ec.Id);
            entity.Property(ec => ec.SubjectName);
            entity.Property(ec => ec.IsAutoCreate);
            entity.Property(ec => ec.GroupCode);
        });

        base.OnModelCreating(modelBuilder);
    }
}