using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KitaplikApp.Data; 
using KitaplikApp.Models; 
using System.Security.Claims; 
using Microsoft.AspNetCore.Authorization; 

namespace KitaplikApp.Controllers
{
    [Authorize(Roles = "Kullanici, Satici")] 
    public class FavorilerController : Controller
    {
        private readonly KitaplikDbContext _context; 

        public FavorilerController(KitaplikDbContext context)
        {
            _context = context;
        }

        // GET: Favoriler 
        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı oturumu bulunamadı. Lütfen giriş yapınız.";
                return RedirectToAction("Login", "Account"); 
            }
            var userId = int.Parse(userIdClaim.Value);
            var favoriler = await _context.FavoriKitaplar
                                        .Include(f => f.Kitap) 
                                            .ThenInclude(k => k.KitapGorselleri) 
                                        .Where(f => f.KullaniciId == userId) 
                                        .ToListAsync();

            return View(favoriler); 
        }

        // POST: Favoriler/Add 
        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> Add(int kitapId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı oturumu bulunamadı. Lütfen giriş yapınız.";
                return Json(new { success = false, message = "Oturum bulunamadı." });
            }
            var userId = int.Parse(userIdClaim.Value);

            var kitap = await _context.Kitaplar.FindAsync(kitapId);
            if (kitap == null)
            {
                return Json(new { success = false, message = "Kitap bulunamadı." });
            }

            // Kullanıcının bu kitabı zaten favorilemiş olup olmadığını kontrol 
            var existingFavorite = await _context.FavoriKitaplar
                                                .FirstOrDefaultAsync(f => f.KullaniciId == userId && f.KitapId == kitapId);

            if (existingFavorite == null)
            {
                // Yeni FavoriKitaplar nesnesi oluştur ve ekle
                var favori = new FavoriKitaplar
                {
                    KullaniciId = userId,
                    KitapId = kitapId
                };
                _context.FavoriKitaplar.Add(favori);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{kitap.Baslik} favorilerinize eklendi.";
                return Json(new { success = true, message = "Kitap favorilere eklendi." });
            }
            else
            {
                TempData["InfoMessage"] = $"{kitap.Baslik} zaten favorilerinizde.";
                return Json(new { success = false, message = "Kitap zaten favorilerinizde." });
            }
        }

        // POST: Favoriler/Remove 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int kitapId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı oturumu bulunamadı. Lütfen giriş yapınız.";
                return Json(new { success = false, message = "Oturum bulunamadı." });
            }
            var userId = int.Parse(userIdClaim.Value);

            // Favori kaydını bul
            var favori = await _context.FavoriKitaplar
                                        .FirstOrDefaultAsync(f => f.KullaniciId == userId && f.KitapId == kitapId);

            if (favori != null)
            {
                var kitapAdi = (await _context.Kitaplar.FindAsync(kitapId))?.Baslik ?? "Kitap"; 
                _context.FavoriKitaplar.Remove(favori); // Favori kaydını sil
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{kitapAdi} favorilerinizden çıkarıldı.";
                return Json(new { success = true, message = "Kitap favorilerden çıkarıldı." });
            }
            else
            {
                TempData["ErrorMessage"] = "Bu kitap favorilerinizde bulunamadı.";
                return Json(new { success = false, message = "Kitap favorilerinizde bulunamadı." });
            }
        }

        // GET: Favoriler/CheckFavoriDurumu 
        [HttpGet]
        public async Task<IActionResult> CheckFavoriteStatus(int kitapId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Json(new { isFavorite = false, isAuthenticated = false }); //oturum açmamışsa
            }
            var userId = int.Parse(userIdClaim.Value);

            // Kitabın kullanıcının favorilerinde olup olmadığını kontrol et
            var isFavorite = await _context.FavoriKitaplar
                                    .AnyAsync(f => f.KullaniciId == userId && f.KitapId == kitapId);

            return Json(new { isFavorite = isFavorite, isAuthenticated = true }); 
        }
    }
}