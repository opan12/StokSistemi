using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StokSistemi.Data;

namespace StokSistemi.Services
{
    public class PriorityScoreUpdater : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public PriorityScoreUpdater(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var orderQueues = dbContext.Orders.ToList();

                    foreach (var orderQueue in orderQueues)
                    {
                        if (orderQueue.EnqueueTime == default(DateTime))
                        {
                            Console.WriteLine($"Invalid EnqueueTime for OrderQueueId {orderQueue.OrderId}");
                            continue;
                        }

                        var customer = dbContext.Customers.FirstOrDefault(c => c.Id == orderQueue.CustomerId);
                        if (customer == null) continue;

                        // Bekleme süresini hesapla (TimeSpan)
                        TimeSpan timeSpan = DateTime.Now - orderQueue.EnqueueTime;
                        double waitingTimeInSeconds = timeSpan.TotalSeconds;

                        // Öncelik skorunu hesapla
                        orderQueue.PriorityScore = customer.CustomerType == "Premium"
                            ? 15 + (waitingTimeInSeconds * 0.5)
                            : 10 + (waitingTimeInSeconds * 0.5);

                        // Customers tablosunda WaitingTime güncelle
                        customer.WaitingTime = timeSpan; // TimeSpan olarak yazılır
                    }

                    dbContext.SaveChanges();
                }

                await Task.Delay(5000, stoppingToken);
            }
        }


    }
}
