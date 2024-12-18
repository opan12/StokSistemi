namespace StokSistemi.Models
{

    public class OrderQueueItem
    {
        public int OrderId { get; set; }
        public int OrderQueueId { get; set; }
        public string CustomerId { get; set; }
        public int ProductId { get; set; }
        public DateTime EnqueueTime { get; set; }
        public string OrderStatus { get; set; }
        public string CustomerType { get; set; }
        public bool IsProcessed { get; set; } = false; // İşlem tamamlandı mı kontrol bayrağı

        public double PriorityScore { get; set; }
        // Diğer işlemlerle ilgili bilgiler eklenecek
    }



}
