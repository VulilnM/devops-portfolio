using DevOpsPortfolio.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPortfolio.Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Car> Cars => Set<Car>();

    public DbSet<SearchRequest> SearchRequests => Set<SearchRequest>();

    public DbSet<Source> Sources => Set<Source>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Car>(entity =>
        {
            entity.ToTable("Cars");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Make)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.Model)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Year)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<SearchRequest>(entity =>
        {
            entity.ToTable("SearchRequests");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasOne(x => x.Car)
                .WithMany(x => x.SearchRequests)
                .HasForeignKey(x => x.CarId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Source>(entity =>
        {
            entity.ToTable("Sources");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Url)
                .IsRequired();

            entity.Property(x => x.Title)
                .HasMaxLength(300);

            entity.Property(x => x.Domain)
                .HasMaxLength(150);

            entity.Property(x => x.FetchedAt)
                .IsRequired();

            entity.HasOne(x => x.SearchRequest)
                .WithMany(x => x.Sources)
                .HasForeignKey(x => x.SearchRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}