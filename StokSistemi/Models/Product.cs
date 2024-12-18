namespace StokSistemi.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Stock { get; set; }
        public decimal Price { get; set; }

        // Yapıcı (Constructor)
        public Product(int productId, string productName, int stock, decimal price)
        {
            ProductId = productId;
            ProductName = productName;
            Stock = stock;
            Price = price;
        }

        // Varsayılan yapıcı (Eğer EF Core kullanılacaksa bu gerekli)
        public Product() { }
    }
}
