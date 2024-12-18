namespace StokSistemi.Models
{
    using System.Collections.Generic;

  
        public class CustomerViewModel
        {
            public List<Customer> Customers { get; set; }
            public List<Product> Products { get; set; }
            public List<Log> LogEntries { get; set; } // Log kayıtları
        public List<Order> Orders { get; set; } // Log kayıtları

    }

}
