using Microsoft.AspNetCore.Mvc;
using System.Linq;
using StokSistemi.Data;

namespace StokSistemi.Controllers
{
    public class LogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LogController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var logs = _context.Logs
                .OrderByDescending(log => log.TransactionTime)
                .ToList();

            return View(logs);
        }
    }
}
