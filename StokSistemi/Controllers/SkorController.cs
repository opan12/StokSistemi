using Microsoft.AspNetCore.Mvc;
using StokSistemi.Data;
using System.Linq;

namespace StokSistemi.Controllers
{
    public class SkorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SkorController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Orders ve Users tablolarını join ederek Username'i al
            var uniqueOrders = _context.Orders
                .Join(
                    _context.Users,                // Join edilecek tablo
                    order => order.CustomerId,     // Orders.CustomerId ile
                    user => user.Id,               // Users.Id eşleştir
                    (order, user) => new
                    {
                        Username = user.UserName,  // Kullanıcı adı
                        PriorityScore = order.PriorityScore
                    })
                .GroupBy(o => o.Username)         // Username'e göre gruplandır
                .Select(group => new
                {
                    Username = group.Key,
                    PriorityScore = group.Max(o => o.PriorityScore) // En yüksek PriorityScore
                })
                .OrderByDescending(o => o.PriorityScore) // Öncelik skoruna göre sırala
                .ToList();

            return View(uniqueOrders);
        }
    }
}
