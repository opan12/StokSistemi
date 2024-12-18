using StokSistemi.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace StokSistemi.Service
{




    public class OrderService
    {
        private readonly LogService _logService;

        public OrderService(LogService logService)
        {
            _logService = logService;
        }

        public string ProcessOrder(Customer customer, Product product, int quantity)
        {
            try
            {
                // Zaman aşımı simülasyonu (asenkron kod yerine senkron bekleme yapın)
                Thread.Sleep(TimeSpan.FromSeconds(15)); // 15 saniye bekler

                // Zaman aşımı iptal edilmediyse işlem devam eder
                if (product.Stock < quantity)
                {
                    _logService.Log(customer.Id, "Hata", customer.CustomerType, product.ProductName, quantity, "Ürün stoğu yetersiz");
                    return "Yetersiz stok!";
                }

                decimal totalPrice = product.Price * quantity;
                if (customer.Budget < totalPrice)
                {
                    _logService.Log(customer.Id, "Hata", customer.CustomerType, product.ProductName, quantity, "Müşteri bakiyesi yetersiz");
                    return "Yetersiz bakiye!";
                }

                // İşlem tamamla
                customer.Budget -= totalPrice; // Bütçeden düş
                customer.TotalSpent += totalPrice; // Toplam harcamayı güncelle
                product.Stock -= quantity; // Stok güncelle

                // Başarılı işlem log kaydı
                _logService.Log(customer.Id, "Bilgilendirme", customer.CustomerType, product.ProductName, quantity, "Sipariş başarıyla tamamlandı!");
                return "Sipariş başarıyla tamamlandı!";
            }
            catch (Exception ex)
            {
                // Diğer hata durumları
                _logService.Log(customer.Id, "Hata", customer.CustomerType, product.ProductName, quantity, "Veritabanı hatası: " + ex.Message);
                return "Bir hata oluştu!";
            }
        }
    }

}
