using InMemoryCaching.Models;
using Microsoft.EntityFrameworkCore;

namespace InMemoryCaching.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        Database.EnsureCreated(); // auto-migrate on startup
    }

    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasKey(p => p.Id);
    }
}