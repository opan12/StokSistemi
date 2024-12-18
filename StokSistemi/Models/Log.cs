namespace StokSistemi.Models
{
    public class Log

    {
     
            public int LogID { get; set; }
            public string CustomerID { get; set; }
            public string LogType { get; set; } // "Hata", "Uyarı", "Bilgilendirme"
            public string CustomerType { get; set; } // "Premium" veya "Standard"
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public DateTime TransactionTime { get; set; }
            public string ResultMessage { get; set; } // İşlem sonucu mesajı
        

    }
}
