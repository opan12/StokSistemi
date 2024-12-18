using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StokSistemi.Data;

public class WaitingTimeUpdateService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public WaitingTimeUpdateService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // Tüm kullanıcıların WaitingTime değerlerini güncelle
                    var users = context.Users.ToList();

                    foreach (var user in users)
                    {
                        var waitingTimeSpan = DateTime.Now - user.EnqueueTime;
                        user.WaitingTime = waitingTimeSpan;

                        // İsteğe bağlı: PriorityScore'u da güncelle
                        user.PriorityScore = user.CustomerType == "Premium"
                            ? 20 + waitingTimeSpan.TotalSeconds * 0.5
                            : 10 + waitingTimeSpan.TotalSeconds * 0.5;
                    }

                    await context.SaveChangesAsync(stoppingToken);
                }

                // 1 dakikada bir güncelleme yap
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating waiting time: {ex.Message}");
            }
        }
    }
}
