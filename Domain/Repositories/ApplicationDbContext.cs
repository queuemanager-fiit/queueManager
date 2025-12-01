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
        
            // Настройка свойств с private set
            entity.Property(e => e.OccurredOn);
            entity.Property(e => e.FormationTime);
            entity.Property(e => e.DeletionTime);
            entity.Property(e => e.State);
            entity.Property(e => e.GroupId);
        
            // Связи
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Events)
                .HasForeignKey("CategoryId");
        
            entity.HasMany(e => e.Users)
                .WithMany();
        });

        modelBuilder.Entity<EventCategory>(entity =>
        {
            entity.HasKey(ec => ec.Id);
            entity.Property(ec => ec.SubjectName);
            entity.Property(ec => ec.IsAutoCreate);
            entity.Property(ec => ec.GroupId);
        });

        base.OnModelCreating(modelBuilder);
    }
}