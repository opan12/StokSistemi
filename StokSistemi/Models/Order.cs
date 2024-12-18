namespace StokSistemi.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public string CustomerId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime OrderDate { get; set; }

        public string OrderStatus { get; set; } // "Pending", "Completed", "Cancelled"
    public double PriorityScore { get; set; }

        public bool IsProcessed { get; set; }
        public DateTime EnqueueTime { get; internal set; }
    }

}
