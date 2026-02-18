using Microsoft.EntityFrameworkCore;
using Domain.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data
{

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<TelephoneNumber> TelephoneNumbers { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Enable EF Core logging to console
            optionsBuilder
                .LogTo(Console.WriteLine, LogLevel.Information)
                .EnableSensitiveDataLogging();

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(b =>
            {
                b.HasKey(c => c.Id);
                b.Property(c => c.Id).ValueGeneratedOnAdd();
                b.Property(c => c.Name).HasMaxLength(200);
                b.Property(c => c.Email).HasMaxLength(200);
                b.HasMany(c => c.PhoneNumbers).WithOne().HasForeignKey(t => t.CustomerId);
                b.HasIndex(c => c.Email).IsUnique();
            });

            modelBuilder.Entity<Product>(b =>
            {
                b.HasKey(p => p.Id);
                b.Property(p => p.Id).ValueGeneratedOnAdd();
                b.Property(p => p.ProductCode).IsRequired();
                b.Property(p => p.Name).IsRequired().HasMaxLength(200);
                b.HasIndex(p => p.ProductCode).IsUnique();
            });

            modelBuilder.Entity<TelephoneNumber>(b =>
            {
                b.HasKey(t => t.Id);
                b.Property(t => t.Id).ValueGeneratedOnAdd();
                b.Property(t => t.Number).HasMaxLength(50);
                b.Property(t => t.Type).HasMaxLength(20);
                b.Property(t => t.CustomerId).IsRequired();
                b.ToTable(t => t.HasCheckConstraint("CK_TelephoneNumber_Type", "Type IN ('Mobile', 'Work', 'DirectDial')"));
            });

            modelBuilder.Entity<Supplier>(b =>
            {
                b.HasKey(s => s.Id);
                b.Property(s => s.Id).ValueGeneratedOnAdd();
                b.Property(s => s.Name).IsRequired().HasMaxLength(200);
                b.HasMany(s => s.Orders).WithOne(o => o.Supplier).HasForeignKey(o => o.SupplierId);
                b.HasIndex(s => s.Name).IsUnique();
                
                // Seed default suppliers
                b.HasData(
                    new Supplier { Id = 1, Name = "Speedy" },
                    new Supplier { Id = 2, Name = "Vault" }
                );
            });

            modelBuilder.Entity<Order>(b =>
            {
                b.HasKey(o => o.Id);
                b.Property(o => o.Id).ValueGeneratedOnAdd();
                b.Property(o => o.CustomerId);
                b.Property(o => o.SupplierId).IsRequired();
                b.Property(o => o.OrderDate).IsRequired();
                b.Property(o => o.CustomerEmail).HasMaxLength(200);
                b.Property(o => o.OrderStatus).IsRequired();
                b.HasMany(o => o.OrderItems).WithOne().HasForeignKey(oi => oi.OrderId).OnDelete(DeleteBehavior.Restrict);
                b.Ignore(o => o.TotalAmount);
                
                // Configure BillingAddress as owned entity (REQUIRED)
                b.OwnsOne(o => o.BillingAddress, a =>
                {
                    a.Property(addr => addr.Street).IsRequired().HasMaxLength(200);
                    a.Property(addr => addr.City).HasMaxLength(100);
                    a.Property(addr => addr.County).HasMaxLength(100);
                    a.Property(addr => addr.PostalCode).HasMaxLength(20);
                    a.Property(addr => addr.Country).HasMaxLength(100);
                });
                
                // Make BillingAddress navigation property required
                b.Navigation(o => o.BillingAddress).IsRequired();
                
                // Configure DeliveryAddress as owned entity (OPTIONAL)
                b.OwnsOne(o => o.DeliveryAddress, a =>
                {
                    a.Property(addr => addr.Street).IsRequired().HasMaxLength(200);
                    a.Property(addr => addr.City).HasMaxLength(100);
                    a.Property(addr => addr.County).HasMaxLength(100);
                    a.Property(addr => addr.PostalCode).HasMaxLength(20);
                    a.Property(addr => addr.Country).HasMaxLength(100);
                });
            });

            modelBuilder.Entity<OrderItem>(b =>
            {
                b.HasKey(oi => oi.Id);
                b.Property(oi => oi.Id).ValueGeneratedOnAdd();
                b.Property(oi => oi.OrderId).IsRequired();
                b.Property(oi => oi.ProductId).IsRequired();
                b.Property(oi => oi.Quantity).IsRequired();
                b.Property(oi => oi.Price).HasColumnType("decimal(18,2)").IsRequired();
                b.HasOne(oi => oi.Product).WithMany().HasForeignKey(oi => oi.ProductId);
                b.ToTable(t => t.HasCheckConstraint("CK_OrderItem_Quantity", "Quantity > 0"));
                b.ToTable(t => t.HasCheckConstraint("CK_OrderItem_Price", "Price >= 0"));
            });
        }
    }
}
