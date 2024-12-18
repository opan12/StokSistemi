using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StokSistemi.Data;
using StokSistemi.Models;

namespace StokSistemi.Services
{
    public class OrderCancellationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public OrderCancellationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    try
                    {
                        var thresholdTime = DateTime.Now.AddSeconds(-1500); // 15 saniye önceki zaman

                        var expiredOrders = dbContext.Orders
                            .Where(o => o.OrderDate <= thresholdTime && o.OrderStatus == "Pending")
                            .ToList();

                        if (expiredOrders.Any())
                        {
                            foreach (var order in expiredOrders)
                            {
                                try
                                {
                                    // Log kaydını ekleme
                                    dbContext.Logs.Add(new Log
                                    {
                                        CustomerID = order.CustomerId,
                                        LogType = "Zaman Aşımı",
                                        CustomerType = dbContext.Customers.FirstOrDefault(c => c.Id == order.CustomerId)?.CustomerType ?? "Unknown",
                                        ProductName = dbContext.Products.FirstOrDefault(p => p.ProductId == order.ProductId)?.ProductName ?? "Unknown Product",
                                        Quantity = order.Quantity,
                                        TransactionTime = DateTime.Now,
                                        ResultMessage = $"Sipariş {order.OrderId} zaman aşımı nedeniyle iptal edildi."
                                    });

                                    Console.WriteLine($"Sipariş {order.OrderId} zaman aşımı nedeniyle iptal edildi.");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Log eklenirken hata oluştu: {ex.Message}");
                                }
                            }

                            // İptal edilen siparişleri veritabanından kaldır
                            dbContext.Orders.RemoveRange(expiredOrders);
                            await dbContext.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"OrderCancellationService hata: {ex.Message}");
                    }
                }

                await Task.Delay(1000, stoppingToken); // 1 saniyelik bekleme
            }
        }
    }
}
