using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StokSistemi.Data;
using StokSistemi.Models;
using StokSistemi.Services;
using System.Threading;
using StokSistemi.Service;

namespace StokSistemi.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdminService _adminService;
        private static readonly Mutex _mutex = new Mutex(); // Mutex nesnesi
        private readonly ApplicationDbContext _context;

        public AdminController(AdminService adminService, ApplicationDbContext context)
        {
            _adminService = adminService;
            _context = context;
        }

        // Tüm ürünleri listele
        public IActionResult Index()
        {
            var products = _adminService.GetAllProducts(); // Senkron olarak ürünleri getir
            return View(products);
        }

        public IActionResult PendingOrders()
        {
            var orders = _context.Orders.Where(o => o.OrderStatus == "Pending").ToList();
            return View(orders); // orders listesini model olarak döndürün
        }

        // Ürün ekleme formu (GET)
        public IActionResult Create()
        {
            return View(); // Ürün ekleme formunu döner
        }

        // Ürün ekleme işlemi (POST)
        // Ürün ekleme işlemi (POST)
        [HttpPost]
        public IActionResult Create(Product product)
        {
            _mutex.WaitOne(); // Mutex'i beklemeye al
            try
            {
                if (ModelState.IsValid)
                {
                    _adminService.AddProduct(product); // Ürünü ekle
                                                       // Ürün başarıyla eklendikten sonra kullanıcıya sipariş vermesi için yönlendirme yapabiliriz.
                    TempData["SuccessMessage"] = "Ürün başarıyla eklendi. Kullanıcılar sipariş verebilir.";
                    return RedirectToAction("Index"); // Başarılı ekleme sonrası yönlendirme
                }
                return View(product); // Model geçerli değilse formu geri döner
            }
            finally
            {
                _mutex.ReleaseMutex(); // Mutex'i serbest bırak
            }
        }

        // Ürün güncelleme formu
        public IActionResult Edit(int id)
        {
            var product = _adminService.GetAllProducts().FirstOrDefault(p => p.ProductId == id);
            if (product == null)
            {
                return NotFound(); // Ürün bulunamazsa hata döndür
            }
            return View(product);
        }

        // Ürün güncelleme işlemi
        [HttpPost]
        public IActionResult Edit(Product product)
        {
            _mutex.WaitOne(); // Mutex'i beklemeye al
            try
            {
                if (ModelState.IsValid)
                {
                    _adminService.UpdateProduct(product); // Ürünü güncelle
                    return RedirectToAction("Index");
                }
                return View(product);
            }
            finally
            {
                _mutex.ReleaseMutex(); // Mutex'i serbest bırak
            }
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult ApproveAllOrders()
        {
            var pendingOrders = _context.Orders
                .Where(o => o.OrderStatus == "Pending")
                .OrderByDescending(o => o.PriorityScore)
                .ToList();

            if (!pendingOrders.Any())
            {
                return Json(new { success = false, message = "Onay bekleyen sipariş bulunamadı." });
            }

            foreach (var order in pendingOrders)
            {
                lock (_mutex)
                {
                    order.OrderStatus = "Onaylandı";
                    _context.SaveChanges();
                    ProcessOrder(order);



                }

            }

            return Json(new { success = true, message = $" sipariş başarıyla onaylandı." });
        }

        private void ProcessOrder(Order orderQueue)
        {
            lock (_context.Orders) // Kilitleme işlemi
            {
                var customer = _context.Customers.Find(orderQueue.CustomerId);
                var product = _adminService.GetProduct(orderQueue.ProductId);

                if (customer == null || product == null)
                {
                    orderQueue.OrderStatus = "Hata: Müşteri veya ürün bulunamadı.";
                    _context.SaveChanges();
                    return;
                }

                // Stok kontrolü
                OrderProcessingSystem.StockMutex.WaitOne();
                try
                {
                    if (product.Stock >= orderQueue.Quantity)
                    {
                        product.Stock -= orderQueue.Quantity; // Stok düşüşü
                        orderQueue.OrderStatus = "Tamamlandı";

                        customer.Budget -= product.Price * orderQueue.Quantity; // Müşteri bütçesinden düşüş
                        customer.TotalSpent += product.Price * orderQueue.Quantity; // Müşteri harcamalarının artışı

                        // Müşteri tipi güncelleniyor
                        if (customer.TotalSpent >= 2000 && customer.CustomerType == "Standard")
                        {
                            customer.CustomerType = "Premium";
                        }

                        _adminService.UpdateProduct(product); // Ürünü güncelle
                    }
                    else
                    {
                        orderQueue.OrderStatus = "Stok Yetersiz";
                    }

                    if (customer.Budget < product.Price * orderQueue.Quantity)
                    {
                        orderQueue.OrderStatus = "Eksik Bütçe";
                    }

                    orderQueue.IsProcessed = true;
                    _context.SaveChanges();
                }
                finally
                {
                    OrderProcessingSystem.StockMutex.ReleaseMutex();
                }
            }
        }

            



            public IActionResult CheckAllOrders()
        {
            var orders = _context.Orders
                .Select(o => new
                {
                    o.OrderId,
                    o.CustomerId,
                    o.OrderStatus,
                    o.OrderDate,
                    o.Quantity,
                    o.ProductId
                })
                .ToList();

            return Json(new { success = true, orders });
        }


        [HttpGet]
        public IActionResult AllOrders()
        {
            return View("AllOrders");
        }
        public static class OrderProcessingSystem
        {
            public static readonly Queue<Customer> CustomerQueue = new Queue<Customer>();
            public static readonly object QueueLock = new object(); // Eşzamanlı erişim için
            public static readonly Queue<Order> OrderQueue = new Queue<Order>(); // Sipariş kuyruğu
            public static readonly Mutex StockMutex = new Mutex(); // Stok güncelleme için Mutex
            public static bool IsProcessing = false; // İşleme durumu
        }


        // Ürün silme işlemi
        [HttpPost]
        public IActionResult Delete(int id)
        {
            _mutex.WaitOne(); // Mutex'i beklemeye al
            try
            {
                _adminService.DeleteProduct(id); // Ürünü sil
                return RedirectToAction("Index");
            }
            finally
            {
                _mutex.ReleaseMutex(); // Mutex'i serbest bırak
            }
        }
    }
}
