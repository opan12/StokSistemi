using Microsoft.AspNetCore.Mvc;

namespace StokSistemi.Controllers
{
    public class CustomerAreaController : Controller
    {
        public IActionResult Index()
        {
            return View(); // İlgili sayfayı döndür
        }
    }

}
