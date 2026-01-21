using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KitaplikApp.Data;
using System.Threading.Tasks;

namespace KitaplikApp.Controllers
{
    [Authorize] 
    public class DashboardController : Controller
    {
        private readonly KitaplikDbContext _context;

        public DashboardController(KitaplikDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int kullaniciId))
            {
                return RedirectToAction("Login", "Account");
            }
            
            var kullanici = await _context.Kullanicilar.FindAsync(kullaniciId);
            if (kullanici == null)
            {
                return NotFound();
            }

            // Sipariş sayısı
            int siparisSayisi = await _context.Siparisler.CountAsync(s => s.AliciKullaniciId == kullaniciId);
            ViewBag.SiparisSayisi = siparisSayisi;

            var sonSiparisler = await _context.Siparisler
                                            .Include(s => s.SiparisDetaylaris!)
                                            .ThenInclude(sd => sd.Kitap!)
                                            .Where(s => s.AliciKullaniciId == kullaniciId)
                                            .OrderByDescending(s => s.SiparisTarihi)
                                            .Take(5)
                                            .ToListAsync();
            
            ViewBag.SonSiparisler = sonSiparisler;

            return View(kullanici);
        }
    }
}