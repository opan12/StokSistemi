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

        public IActionResult Index()
        {
            if (SystemState.IsAdminProcessing)
            {
                return Json(new { success = false, message = "Admin işlem yapıyor. Lütfen daha sonra tekrar deneyin." });
            }
            var model = new CustomerViewModel
            {
               // Customers = _customerQueue.GetAllCustomers(), // Tüm müşterileri al
                Products = _adminService.GetAllProducts(), // Tüm ürünleri al
                LogEntries = _logService.GetAllLogs() // Tüm logları al
            };

            return View(model); // Model ile Index.cshtml'yi döndür
        }
        public IActionResult PlaceOrder(List<Order> orders)
        {
            _mutex.WaitOne();
            try
            {
                var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var customer = _context.Customers.Find(customerId);

                if (customer == null)
                {
                    return Json(new { success = false, message = "Müşteri bulunamadı." });
                }

                decimal totalSpent = 0;

                foreach (var order in orders)
                {
                    if (order.Quantity <= 0)
                    {
                        continue;
                    }

                    var product = _adminService.GetProduct(order.ProductId);
                    if (product == null)
                    {
                        return Json(new { success = false, message = $"Ürün ID {order.ProductId} bulunamadı." });
                    }

                    if (product.Stock < order.Quantity)
                    {
                        return Json(new { success = false, message = $"Yetersiz stok: {product.Stock} adet mevcut." });
                    }

                    var newOrder = CreateOrder(customerId, order, product);
                    newOrder.EnqueueTime = DateTime.Now; // Kuyruğa eklenme zamanı

                    _context.Orders.Add(newOrder);

                    EnqueueOrderToDb(newOrder); // Siparişi kuyruğa ekle ve öncelik skorunu hesapla


                }

               // customer.TotalSpent += totalSpent;

                if (customer.TotalSpent >= 2000 && customer.CustomerType != "Premium")
                {
                    customer.CustomerType = "Premium";
                }

          

                _context.SaveChanges();

                return Json(new { success = true, message = "Sipariş(ler) başarıyla verildi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Bir hata oluştu: {ex.Message}" });
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        // Sipariş kuyruğuna siparişi ekleyen metot

        public void EnqueueOrderToDb(Order order)
        {
            // Öncelik skorunu hesapla ve ata
            order.PriorityScore = CalculatePriorityScore(order);

            _context.Orders.Add(order); // Veritabanına ekle
            _context.SaveChanges(); // Değişiklikleri kaydet
        }

        // Öncelik skoru hesaplama
        public double CalculatePriorityScore(Order order)
        {
            // Veritabanından müşteri bilgilerini al
            var customer = _context.Customers.FirstOrDefault(c => c.Id == order.CustomerId);

            if (customer == null)
            {
                throw new Exception("Customer not found."); // Eğer müşteri bulunamazsa hata fırlat
            }

            // Bekleme süresini saniye cinsinden hesapla
            TimeSpan timeSpan = DateTime.Now - order.EnqueueTime;
            double waitingTimeInSeconds = timeSpan.TotalSeconds;


            // Öncelik skorunu müşteri tipine ve bekleme süresine göre hesapla
            double priorityScore = customer.CustomerType == "Premium"
                ? 15 + (waitingTimeInSeconds * 0.5) // Premium müşteriler için
                : 10 + (waitingTimeInSeconds * 0.5); // Standart müşteriler için

            // Veritabanındaki PriorityScore değerini güncelle
            var orderQueueFromDb = _context.Orders.FirstOrDefault(o => o.OrderId == order.OrderId);
            if (orderQueueFromDb != null)
            {
                orderQueueFromDb.PriorityScore = priorityScore;
                _context.SaveChanges(); // Güncellemeyi veritabanına kaydet
            }

            return priorityScore;
        }




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
                    o.ProductId
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
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                return Unauthorized("Kullanıcı girişi yapılmadı veya bulunamadı."); // Kullanıcı bulunamazsa yetkisiz erişim
            }

            return View(user); // Kullanıcı bilgilerini göster
        }
    }
}
 
