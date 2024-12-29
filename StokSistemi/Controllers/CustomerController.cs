using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading;
using StokSistemi.Data;
using StokSistemi.Models;
using StokSistemi.Service;
using StokSistemi.Services;
using static StokSistemi.Controllers.AdminController;

namespace StokSistemi.Controllers
{
    public class CustomerController : Controller
    {
        private readonly AdminService _adminService;
        private readonly CustomerQueue _customerQueue;
        private readonly OrderService _orderService;
        private readonly LogService _logService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Customer> _userManager;
        private static readonly Mutex _mutex = new Mutex(); // Mutex nesnesi

        public CustomerController(AdminService adminService, CustomerQueue customerQueue, OrderService orderService, LogService logService, ApplicationDbContext context, UserManager<Customer> userManager)
        {
            _adminService = adminService;
            _customerQueue = customerQueue;
            _orderService = orderService;
            _logService = logService;
            _context = context;
            _userManager = userManager;
        }
        public static class SystemState
        {
            public static bool IsAdminProcessing { get; set; } = false;
        }



        [HttpGet]
        public IActionResult CheckRedirect()
        {
            var currentPage = HttpContext.Session.GetString("CurrentPage");

            // Eğer kullanıcı ProductIndex'e geçtiyse
            if (currentPage == "ProductIndex")
            {
                return Json(new { redirect = true });
            }

            return Json(new { redirect = false });
        }

        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public async Task<IActionResult> Index()
        {
            var currentPage = HttpContext.Session.GetString("CurrentPage");

            // Eğer `ProductIndex` oturumda set edilmişse, erişimi engelle
            if (currentPage == "ProductIndex")
            {
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    TempData["ErrorMessage"] = "Admin işlem yapıyor lütfen bekleyin."; // Hata mesajını sakla
                    return Redirect(referer); // Önceki sayfaya yönlendir
                }

                // Eğer `Referer` yoksa hiçbir şey yapma ve mevcut sayfada kal
                return new EmptyResult(); // Hiçbir işlem yapılmaz
            }

            // `CurrentPage` oturum bilgisini güncelle
            HttpContext.Session.SetString("CurrentPage", "CustomerIndex");

            await _semaphore.WaitAsync();

            try
            {
                var model = new CustomerViewModel
                {
                    Products = _adminService.GetAllProducts(),
                    LogEntries = _logService.GetAllLogs()
                };

                return View(model);
            }
            finally
            {
                _semaphore.Release();
            }
        }








        public IActionResult PlaceOrder(List<Order> orders)
        {
            _mutex.WaitOne(); // Mutex kilidi alınıyor
            try
            {
                var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var customer = _context.Customers.Find(customerId);

                if (customer == null)
                {
                    AddLog(customerId, "PlaceOrder", "N/A", "N/A", 0, "Müşteri bulunamadı.");
                    TempData["ErrorMessage"] = "Müşteri bulunamadı.";
                    return RedirectToAction("OrderPage"); // İlgili sayfaya yönlendirme
                }

                string lastProductName = "N/A";

                foreach (var order in orders)
                {
                    if (order.Quantity <= 0)
                        continue;

                    var product = _adminService.GetProduct(order.ProductId);
                    if (product == null)
                    {
                        AddLog(customerId, "PlaceOrder", customer.CustomerType, order.ProductId.ToString(), order.Quantity, $"Ürün ID {order.ProductId} bulunamadı.");
                        TempData["ErrorMessage"] = $"Ürün ID {order.ProductId} bulunamadı.";
                        return RedirectToAction("Index");
                    }

                    if (product.Stock < order.Quantity)
                    {
                        AddLog(customerId, "PlaceOrder", customer.CustomerType, product.ProductName, order.Quantity, $"Yetersiz stok: {product.Stock} adet mevcut.");
                        TempData["ErrorMessage"] = $"Yetersiz stok: {product.Stock} adet mevcut.";
                        return RedirectToAction("Index");
                    }

                    var totalOrderedQuantity = _context.Orders
                        .Where(o => o.CustomerId == customerId && o.ProductId == order.ProductId)
                        .Sum(o => o.Quantity);

                    if (totalOrderedQuantity + order.Quantity > 5)
                    {
                        TempData["ErrorMessage"] = $"Ürün ID {order.ProductId} için toplamda en fazla 5 adet sipariş verebilirsiniz. Daha önce {totalOrderedQuantity} adet sipariş verdiniz.";
                        return RedirectToAction("Index");
                    }

                    lastProductName = product.ProductName; // En son işlenen ürünün adını saklıyoruz

                    // Siparişi oluştur
                    var newOrder = CreateOrder(customerId, order, product);
                    newOrder.EnqueueTime = DateTime.Now;

                    _context.Orders.Add(newOrder);
                }

                _context.SaveChanges();

                AddLog(customerId, "PlaceOrder", customer.CustomerType, lastProductName, orders.Count, "Sipariş(ler) başarıyla verildi."); // Burada lastProductName kullanılıyor
                TempData["SuccessMessage"] = "Sipariş(ler) başarıyla verildi.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                AddLog("N/A", "PlaceOrder", "N/A", "N/A", 0, $"Bir hata oluştu: {ex.Message}");
                TempData["ErrorMessage"] = $"Bir hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
            finally
            {
                _mutex.ReleaseMutex(); // Mutex kilidi serbest bırakılıyor
            }
        }


        // Sipariş kuyruğuna siparişi ekleyen metot



        // Öncelik skoru hesaplama





        //private void UpdateCustomerQueue(YAZLAB2.Service.CustomerQueue customerQueue)
        //{
        //    var queue = customerQueue.GetAllCustomers();

        //    foreach (var queuedCustomer in queue)
        //    {
        //        var waitTime = (DateTime.Now - queuedCustomer.EnqueueTime).TotalSeconds;
        //        var basePriority = queuedCustomer.CustomerType == "Premium" ? 20 : 10;
        //        var priorityScore = basePriority + (waitTime * 0.5);

        //        queuedCustomer.PriorityScore = priorityScore;
        //    }

        //    var sortedQueue = queue.OrderByDescending(c => c.PriorityScore).ToList();

        //    foreach (var customer in sortedQueue)
        //    {
        //        customerQueue.AddCustomerToQueue(customer);
        //    }
        //}

        private Order CreateOrder(string customerId, Order order, Product product)
        {
            return new Order
            {
                CustomerId = customerId,
                ProductId = order.ProductId,
                Quantity = order.Quantity,
                TotalPrice = product.Price * order.Quantity,
                OrderDate = DateTime.Now,
                OrderStatus = "Pending"

            };
        }

        [HttpGet]
        public IActionResult CheckOrder()
        {
            return View("CheckOrder");
        }
        [HttpGet]
        public IActionResult Sepetim()
        {
            return View("Sepetim");
        }
        [HttpGet]
        public IActionResult Check()
        {
            return View("Check");
        }

        [HttpGet]
        public async Task<IActionResult> CheckOrderStatus()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Json(new { success = false, message = "Kullanıcı oturumu bulunamadı." });
            }

            var customerId = user.Id;

            var orders = _context.Orders
                .Where(o => o.CustomerId == customerId)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderDate,
                    o.Quantity,
                    o.ProductId,
                            o.OrderStatus,
                            o.PriorityScore


                }).ToList();

            if (!orders.Any())
            {
                return Json(new { success = false, message = "Henüz siparişiniz bulunmuyor." });
            }

            return Json(new { success = true, orders });
        }
    


    [HttpGet]
        public async Task<IActionResult> CustomerArea(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                // Kullanıcı adı boşsa veya null ise bir hata döndür veya bir başka sayfaya yönlendir
                return BadRequest("Hata lütfen daha sonra tekrar deneyin.");
            }

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Kullanıcı işlemlerine devam et
            return View(user);
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
 
