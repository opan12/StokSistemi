using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StokSistemi.Models;

namespace StokSistemi.Data
{
    public class ApplicationDbContext : IdentityDbContext<Customer>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DB Set'ler
        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Log> Logs { get; set; }

        public DbSet<Order> Orders { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Başlangıç ürün verileri
            modelBuilder.Entity<Product>().HasData(
                new Product { ProductId = 1, ProductName = "Product1", Stock = 500, Price = 100 },
                new Product { ProductId = 2, ProductName = "Product2", Stock = 10, Price = 50 },
                new Product { ProductId = 3, ProductName = "Product3", Stock = 200, Price = 45 },
                new Product { ProductId = 4, ProductName = "Product4", Stock = 75, Price = 75 },
                new Product { ProductId = 5, ProductName = "Product5", Stock = 0, Price = 500 }
            );


            // Fiyatın hassasiyetini ayarlama
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");
          
        }
    }
}
