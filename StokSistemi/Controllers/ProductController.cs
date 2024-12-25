using Microsoft.AspNetCore.Mvc;
using StokSistemi.Data;
using StokSistemi.Models;
using System.Threading;

namespace StokSistemi.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        private static Mutex _mutex = new Mutex();
        public IActionResult Index()
        {
            HttpContext.Session.SetString("CurrentPage", "ProductIndex");

            if (!_mutex.WaitOne(0))
            {
                return Json(new { success = false, message = "Başka bir işlem devam ediyor." });
            }

            try
            {
                var products = _context.Products.ToList();
                return View(products);
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        public IActionResult BackToPreviousPage()
        {
            // CurrentPage oturum bilgisini temizle
            HttpContext.Session.Remove("CurrentPage");

            // Log ile kontrol
            Console.WriteLine("CurrentPage değeri temizlendi.");

            // Önceki sayfaya yönlendir
            return RedirectToAction("Index", "Admin"); // Burayı istediğiniz önceki sayfa ile değiştirin
        }




        // Detay Gösterimi
        public IActionResult Details(int id)
        {
            var product = _context.Products.Find(id); // Ürün ID'ye göre detay göster
            if (product == null)
            {
                return NotFound(); // Eğer ürün bulunamazsa 404 hata döner
            }
            return View(product);
        }

        public IActionResult Edit(int id)
        {
            var product = _context.Products.Find(id); // Güncellenecek ürünü bul
            if (product == null)
            {
                return NotFound(); // Eğer ürün bulunamazsa 404 hata döner
            }
            return View(product); // Edit sayfasına gönder
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Product product)
        {
            if (product == null)
            {
                return NotFound(); // Ürün bulunamazsa 404 hatası döndür
            }

            try
            {
                _context.Products.Update(product); // Ürün bilgilerini güncelle
                _context.SaveChanges(); // Veritabanına kaydet

                // Güncellenen ürün bilgilerini tekrar almak
                var updatedProduct = _context.Products.Find(product.ProductId);
                return RedirectToAction(nameof(Index), new { updatedProduct.ProductId });
            }
            catch (Exception ex)
            {
                // Hata durumunda işlemler
                // Loglama veya uygun şekilde hatayı gösterebilirsiniz
            }
            return View(product); // Hata varsa formu tekrar göster
        }
        public IActionResult Delete(int id)
        {
            var product = _context.Products.Find(id); // Silinecek ürünü bul
            if (product == null)
            {
                return NotFound(); // Eğer ürün bulunamazsa 404 döndür
            }

            _context.Products.Remove(product); // Ürünü veritabanından kaldır
            _context.SaveChanges(); // Veritabanına değişiklikleri kaydet

            return RedirectToAction(nameof(Index)); // Silme işlemi sonrası index sayfasına yönlendir
        }


    }
}