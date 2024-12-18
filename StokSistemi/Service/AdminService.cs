using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using StokSistemi.Data;
using StokSistemi.Models;

namespace StokSistemi.Services
{
    public class AdminService
    {
        private readonly ApplicationDbContext _context;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Eşzamanlılık kontrolü

        public AdminService(ApplicationDbContext context)
        {
            _context = context;
        }
        public Product GetProduct(int productId)
        {
            return _context.Products.Find(productId); // Bu kullanımda DbContext artık disposable hale gelir
        }
        // Tüm ürünleri getir
        public List<Product> GetAllProducts()
        {
            return _context.Products.AsNoTracking().ToList(); // Tüm ürünleri getir
        }

        // Ürün ekleme işlemi
        public void AddProduct(Product product)
        {
            _semaphore.Wait(); // Semaforu al
            try
            {
                _context.Products.Add(product); // Yeni ürünü ekle
                _context.SaveChanges(); // Veritabanında değişiklikleri kaydet
            }
            finally
            {
                _semaphore.Release(); // Semaforu serbest bırak
            }
        }

        // Ürün güncelleme işlemi
        public void UpdateProduct(Product product)
        {
            _semaphore.Wait(); // Semaforu al
            try
            {
                var existingProduct = _context.Products.FirstOrDefault(p => p.ProductId == product.ProductId);
                if (existingProduct != null)
                {
                    // Yeni değişkeni burada tanımlayın
                    var updatedProduct = new Product
                    {
                        ProductId = existingProduct.ProductId,
                        ProductName = product.ProductName,
                        Stock = product.Stock,
                        Price = product.Price
                    };

                    _context.SaveChanges(); // Değişiklikleri kaydet
                }
            }
            finally
            {
                _semaphore.Release(); // Semaforu serbest bırak
            }
        }


        // Ürün silme işlemi
        public void DeleteProduct(int productId)
        {
            _semaphore.Wait(); // Semaforu al
            try
            {
                var product = _context.Products.FirstOrDefault(p => p.ProductId == productId);
                if (product != null)
                {
                    _context.Products.Remove(product);
                    _context.SaveChanges(); // Veritabanında değişiklikleri kaydet
                }
            }
            finally
            {
                _semaphore.Release(); // Semaforu serbest bırak
            }
        }
    }
}
