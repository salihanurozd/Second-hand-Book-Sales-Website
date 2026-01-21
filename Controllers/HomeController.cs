using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using KitaplikApp.Models;
using KitaplikApp.Data;
using Microsoft.EntityFrameworkCore; 
using System.Linq; 

namespace KitaplikApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly KitaplikDbContext _context;

        public HomeController(ILogger<HomeController> logger, KitaplikDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var kitaplar = await _context.Kitaplar
                                        .Include(k => k.Yazar)
                                        .Include(k => k.Yayinevi)
                                        .Include(k => k.Kategori)
                                        .Include(k => k.KitapDurumu)
                                        .Include(k => k.SaticiKullanici)
                                        .Include(k => k.KitapGorselleri)            
                                        .Include(k => k.Yorumlars)! 
                                            .ThenInclude(y => y.Kullanici) 
                                        .ToListAsync();

            return View(kitaplar);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}