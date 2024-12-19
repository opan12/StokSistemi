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
        public static class SystemState
        {
            public static bool IsAdminProcessing { get; set; } = false;
        }

        public IActionResult PendingOrders()
        {
            ViewData["Message"] = TempData["Message"] as string; // TempData'dan mesajı al

            var orders = _context.Orders.Where(o => o.OrderStatus == "Pending").ToList();
            return View(orders); // orders listesini model olarak döndürün
        }

        // Ürün ekleme formu (GET)
        public IActionResult Create()
        {
            SystemState.IsAdminProcessing = true;

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
                SystemState.IsAdminProcessing = false;

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
                TempData["Message"] = "Onay bekleyen sipariş bulunamadı.";
                return RedirectToAction("PendingOrders");
            }

            foreach (var order in pendingOrders)
            {
                lock (_mutex)
                {
                    var customer = _context.Customers.Find(order.CustomerId);
                    var product = _context.Products.Find(order.ProductId);


                    // Log eklemeden önce müşteri ve ürün kontrolü
                    if (customer == null || product == null)
                    {
                        AddLog(
                            customerId: order.CustomerId,
                            logType: "OrderStatus",
                            customerType: customer?.CustomerType ?? "Unknown",
                            productName: product?.ProductName ?? "Unknown",
                            quantity: order.Quantity,
                            resultMessage: "Hata: Müşteri veya ürün bulunamadı."
                        );

                        // Siparişi atla, sonraki siparişe geç
                        continue;
                    }

                    AddLog(
                        customerId: order.CustomerId,
                        logType: "OrderStatus",
                        customerType: customer.CustomerType,
                        productName: product.ProductName,
                        quantity: order.Quantity,
                        resultMessage: "Sipariş işleniyor."
                    );

                    order.OrderStatus = "İşleniyor";
                    _context.SaveChanges();

                    // Siparişi işleme
                    ProcessOrder(order);
                }
            }

            TempData["Message"] = "Siparişler başarıyla onaylandı.";
            return RedirectToAction("PendingOrders"); // İşlem tamamlandıktan sonra onaylanan siparişlerin listelendiği sayfaya yönlendir        }

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

                    AddLog(
                        customerId: orderQueue.CustomerId,
                        logType: "ProcessOrder",
                        customerType: customer?.CustomerType ?? "Unknown",
                        productName: product?.ProductName ?? "Unknown",
                        quantity: orderQueue.Quantity,
                        resultMessage: "Müşteri veya ürün bulunamadı."
                    );

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
                        if (customer.TotalSpent >= 2000 && customer.CustomerType == "Premium")
                        {
                            customer.CustomerType = "Premium";
                        }

                        _adminService.UpdateProduct(product); // Ürünü güncelle



                        AddLog(
                            customerId: orderQueue.CustomerId,
                            logType: "ProcessOrder",
                            customerType: customer.CustomerType,
                            productName: product.ProductName,
                            quantity: orderQueue.Quantity,
                            resultMessage: "Tamamlandı."
                        );

                    }
                    else
                    {
                        orderQueue.OrderStatus = "Stok Yetersiz";
                        AddLog(
                            customerId: orderQueue.CustomerId,
                            logType: "ProcessOrder",
                            customerType: customer.CustomerType,
                            productName: product.ProductName,
                            quantity: orderQueue.Quantity,
                            resultMessage: "Stok yetersiz."
                        );

                    }

                    if (customer.Budget < product.Price * orderQueue.Quantity)
                    {
                        orderQueue.OrderStatus = "Eksik Bütçe";



                        AddLog(
                            customerId: orderQueue.CustomerId,
                            logType: "ProcessOrder",
                            customerType: customer.CustomerType,
                            productName: product.ProductName,
                            quantity: orderQueue.Quantity,
                            resultMessage: "Müşteri bütçesi yetersiz."
                        );

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


        private void AddLog(string customerId, string logType, string customerType, string productName, int quantity, string resultMessage)
        {
            var log = new Log
            {
                CustomerID = customerId ?? "Unknown",
                LogType = logType,
                CustomerType = customerType ?? "Unknown",
                ProductName = productName ?? "Unknown",
                Quantity = quantity,
                TransactionTime = DateTime.Now,
                ResultMessage = resultMessage
            };

            _context.Logs.Add(log);
            _context.SaveChanges();
        }

    }
}

