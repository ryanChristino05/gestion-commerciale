using Microsoft.EntityFrameworkCore;
using GestionCommerciale.Api.Models;

namespace GestionCommerciale.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleItem> SaleItems { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Enums stockés en string (lisibles en base)
        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        modelBuilder.Entity<Sale>()
            .Property(s => s.Status)
            .HasConversion<string>();

        modelBuilder.Entity<StockMovement>()
            .Property(m => m.Type)
            .HasConversion<string>();

        // Précision des décimaux
        modelBuilder.Entity<Product>()
            .Property(p => p.UnitPrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Sale>()
            .Property(s => s.TotalAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<SaleItem>()
            .Property(si => si.UnitPrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<SaleItem>()
            .Property(si => si.Subtotal)
            .HasColumnType("decimal(18,2)");

        // Numéro de vente unique
        modelBuilder.Entity<Sale>()
            .HasIndex(s => s.Number)
            .IsUnique();

        // Référence produit unique
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Reference)
            .IsUnique();

        // Username unique
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // Relation Sale → User (vendeur)
        modelBuilder.Entity<Sale>()
            .HasOne(s => s.Seller)
            .WithMany(u => u.Sales)
            .HasForeignKey(s => s.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relation Sale → Client
        modelBuilder.Entity<Sale>()
            .HasOne(s => s.Client)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}