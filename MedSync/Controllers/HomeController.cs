using System.Diagnostics;
using MedSync.Models;
using Microsoft.AspNetCore.Mvc;

namespace MedSync.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult About()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View("contact");
        }
        public IActionResult Appointment()
        {
            return View("appointment");
        }
        public IActionResult Privacy()
        {
            return View();
        }
        //public IActionResult Register()
        //{
        //    return View();
        //}

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
