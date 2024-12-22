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
            // Orders ve Users tablolarını join ederek WaitingTime > 0 olan kullanıcıları filtrele
            var uniqueOrders = _context.Orders
                .Join(
                    _context.Users,
                    order => order.CustomerId,
                    user => user.Id,
                    (order, user) => new
                    {
                        Username = user.UserName,
                        OrderId = order.OrderId,
                        PriorityScore = order.PriorityScore,
                        WaitingTime = user.WaitingTime // Kullanıcının WaitingTime değeri
                    })
                .AsEnumerable() // Veritabanı sorgusunu belleğe al
                .Where(o => o.WaitingTime.TotalSeconds > 0) // Bellekte TimeSpan özelliğini filtrele
                .GroupBy(o => o.Username) // Username'e göre gruplandır
                .Select(group => group
                    .OrderByDescending(o => o.OrderId) // OrderId'ye göre azalan sırayla sırala
                    .First()) // İlk öğeyi al (OrderId'si en yüksek olan)
                .OrderByDescending(o => o.PriorityScore) // Öncelik skoruna göre sırala
                .ToList();

            return View(uniqueOrders);
        }



    }
}
