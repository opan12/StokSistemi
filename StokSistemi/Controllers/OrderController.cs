using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StokSistemi.Data;
using StokSistemi.Models;
using System;
using System.Linq;

namespace StokSistemi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly object _mutex = new object(); // Thread-safe işlemler için

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Log ekleme fonksiyonu
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

        // Siparişleri onaylama metodu
        [HttpPost]
        public IActionResult ApproveAllOrders()
        {
            var pendingOrders = _context.Orders
                .Where(o => o.OrderStatus == "Pending")
                .OrderByDescending(o => o.PriorityScore)
                .ToList();

            if (!pendingOrders.Any())
            {
                AddLog(
                    customerId: "N/A",
                    logType: "ApproveAllOrders",
                    customerType: "N/A",
                    productName: "N/A",
                    quantity: 0,
                    resultMessage: "Onay bekleyen sipariş bulunamadı."
                );

                return Json(new { success = false, message = "Onay bekleyen sipariş bulunamadı." });
            }

            foreach (var order in pendingOrders)
            {
                lock (_mutex)
                {
                    try
                    {
                        order.OrderStatus = "İşleniyor";
                        _context.SaveChanges();
                        ProcessOrder(order);
                    }
                    catch (Exception ex)
                    {
                        AddLog(
                            customerId: order.CustomerId,
                            logType: "OrderProcessing",
                            customerType: "N/A",
                            productName: "N/A",
                            quantity: order.Quantity,
                            resultMessage: $"Hata: {ex.Message}"
                        );
                    }
                }
            }

            return Json(new { success = true, message = "Tüm siparişler başarıyla onaylandı." });
        }
        [HttpGet("pending")]
        public IActionResult GetPendingOrders()
        {
            // OrderStatus "Pending" olan siparişleri listele
            var pendingOrders = _context.Orders
                .Where(o => o.OrderStatus == "Pending")
                .ToList();

            return Ok(pendingOrders);
        }


        // Sipariş işleme metodu
        private void ProcessOrder(Order orderQueue)
        {
            lock (_context.Orders) // Kilitleme işlemi
            {
                var customer = _context.Customers.Find(orderQueue.CustomerId);
                var product = _context.Products.Find(orderQueue.ProductId);

                if (customer == null || product == null)
                {
                    orderQueue.OrderStatus = "Hata: Müşteri veya ürün bulunamadı.";
                    _context.SaveChanges();

                    AddLog(
                        customerId: orderQueue.CustomerId,
                        logType: "OrderProcessing",
                        customerType: customer?.CustomerType ?? "Unknown",
                        productName: product?.ProductName ?? "Unknown",
                        quantity: orderQueue.Quantity,
                        resultMessage: "Müşteri veya ürün bulunamadı."
                    );

                    return;
                }

                // Sipariş işleme başarılıysa log ekle
                orderQueue.OrderStatus = "Tamamlandı";
                _context.SaveChanges();

                AddLog(
                    customerId: orderQueue.CustomerId,
                    logType: "OrderProcessing",
                    customerType: customer.CustomerType,
                    productName: product.ProductName,
                    quantity: orderQueue.Quantity,
                    resultMessage: "Sipariş başarıyla işlendi."
                );
            }
        }
            [HttpPut("{id}")]
            public IActionResult UpdateOrder(int id, [FromBody] Order updatedOrder)
            {
                if (id != updatedOrder.OrderId)
                {
                    return BadRequest("Sipariş ID'si uyuşmuyor.");
                }

                var existingOrder = _context.Orders.FirstOrDefault(o => o.OrderId == id);
                if (existingOrder == null)
                {
                    return NotFound("Sipariş bulunamadı.");
                }

                // Mevcut siparişi güncelle
                existingOrder.CustomerId = updatedOrder.CustomerId;
                existingOrder.ProductId = updatedOrder.ProductId;
                existingOrder.Quantity = updatedOrder.Quantity;
                existingOrder.TotalPrice = updatedOrder.TotalPrice;
                existingOrder.OrderStatus = updatedOrder.OrderStatus;
                existingOrder.PriorityScore = updatedOrder.PriorityScore;
                existingOrder.IsProcessed = updatedOrder.IsProcessed;

                // Veritabanında değişiklikleri kaydet
                _context.SaveChanges();

                return Ok("Sipariş başarıyla güncellendi.");
            }
        }
    }
    