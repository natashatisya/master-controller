using Master.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Master.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<HostEntity> Hosts { get; set; }
    public DbSet<Deployment> Deployments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HostEntity>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Hostname).IsRequired().HasMaxLength(255);
            entity.Property(h => h.IpAddress).IsRequired().HasMaxLength(45);
            entity.Property(h => h.Status).HasDefaultValue("Offline");
        });

        modelBuilder.Entity<Deployment>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasOne(d => d.Host)
                  .WithMany(h => h.Deployments)
                  .HasForeignKey(d => d.HostId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}