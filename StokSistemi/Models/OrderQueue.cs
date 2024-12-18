using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class OrderQueue
{
    [Key]
    public int OrderQueueId { get; set; }

    [MaxLength(50)]
    public string CustomerId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public double PriorityScore { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime EnqueueTime { get; set; }

    public bool IsProcessed { get; set; } 
}
