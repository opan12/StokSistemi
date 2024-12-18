using System;
using System.Collections.Generic;
using System.Threading;

namespace StokSistemi
{
    public class StockService
    {
        private static Dictionary<int, int> productStocks = new Dictionary<int, int>(); // Ürün stokları
        private static readonly Mutex mutex = new Mutex(); // Mutex ile kritik bölge kontrolü

        // Ürün ekleme
        public void AddProduct(int productId, int stock)
        {
            mutex.WaitOne();
            try
            {
                if (productStocks.ContainsKey(productId))
                    productStocks[productId] += stock;
                else
                    productStocks[productId] = stock;

                Console.WriteLine($"Ürün {productId} eklendi. Stok: {productStocks[productId]}");
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        // Stok azaltma
        public bool PurchaseProduct(int productId, int quantity)
        {
            mutex.WaitOne();
            try
            {
                if (productStocks.ContainsKey(productId) && productStocks[productId] >= quantity)
                {
                    productStocks[productId] -= quantity;
                    Console.WriteLine($"Ürün {productId} satın alındı. Kalan stok: {productStocks[productId]}");
                    return true;
                }
                Console.WriteLine($"Ürün {productId} için yetersiz stok!");
                return false;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        // Stok bilgisi
        public void DisplayStock()
        {
            mutex.WaitOne();
            try
            {
                foreach (var item in productStocks)
                {
                    Console.WriteLine($"Ürün {item.Key}: {item.Value} adet stok.");
                }
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
    }
}
