using Microsoft.AspNetCore.Identity;

namespace StokSistemi.Models
{
    public class Customer : IdentityUser
    {
        public decimal Budget { get; set; } // Bakiye
        public string CustomerType { get; set; } // "Premium" veya "Standard"
        public decimal TotalSpent { get; set; } // Toplam harcama
        public double PriorityScore { get; set; } // Dinamik öncelik skoru
        public TimeSpan WaitingTime { get; set; } // Bekleme süresi
        public DateTime EnqueueTime { get; set; } // Kuyruğa eklenme zamanı


        // Öncelik Skoru Hesaplama
        public void UpdatePriorityScore()
        {
            double baseScore = CustomerType == "Premium" ? 20 : 10;
            double waitingWeight = 0.5; // Bekleme süresi ağırlığı
            PriorityScore = baseScore + (WaitingTime.TotalSeconds * waitingWeight);
        }

        // Premium Statüsüne Geçiş

    }
}