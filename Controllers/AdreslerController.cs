using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KitaplikApp.Data;
using KitaplikApp.Models;

namespace KitaplikApp.Controllers
{
    [Authorize]
    public class AdreslerController : Controller
    {
        private readonly KitaplikDbContext _context;

        public AdreslerController(KitaplikDbContext context)
        {
            _context = context;
        }
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }
        // CRUD

        // GET: Adresler/Index
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "Kullanıcı oturumu bulunamadı. Lütfen giriş yapınız.";
                return RedirectToAction("Login", "Account");
            }

            var userAddresses = await _context.Adresler
                                              .Where(a => a.KullaniciId == userId.Value)
                                              .ToListAsync();
            return View(userAddresses);
        }
        
        // POST: Adresler/Create 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AdresBasligi,Sehir,Ilce,AcikAdres,PostaKodu")] Adresler adres)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "Kullanıcı oturumu bulunamadı." });
            }
            
            if (ModelState.IsValid)
            {
                adres.KullaniciId = userId.Value;
                _context.Add(adres);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Adres başarıyla eklendi.", adres = adres });
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Adres kaydedilirken bir hata oluştu.", errors = errors });
        }

        // POST: Adresler/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AdresId,AdresBasligi,Sehir,Ilce,AcikAdres,PostaKodu")] Adresler adres)
        {
            if (id != adres.AdresId)
            {
                return Json(new { success = false, message = "Adres bulunamadı." });
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "Kullanıcı oturumu bulunamadı." });
            }

            var existingAddress = await _context.Adresler.AsNoTracking().FirstOrDefaultAsync(a => a.AdresId == id);
            if (existingAddress == null || existingAddress.KullaniciId != userId.Value)
            {
                return Json(new { success = false, message = "Bu adresi düzenleme yetkiniz yok." });
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    adres.KullaniciId = userId.Value;
                    _context.Update(adres);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Adres başarıyla güncellendi.", updatedAdres = adres });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AdresExists(adres.AdresId))
                    {
                        return Json(new { success = false, message = "Adres bulunamadı." });
                    }
                    throw;
                }
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Adres güncellenirken bir hata oluştu.", errors = errors });
        }

        // POST: Adresler/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "Kullanıcı oturumu bulunamadı." });
            }

            var adres = await _context.Adresler.FirstOrDefaultAsync(a => a.AdresId == id && a.KullaniciId == userId.Value);
            
            if (adres != null)
            {
                _context.Adresler.Remove(adres);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Adres başarıyla silindi." });
            }
            else
            {
                return Json(new { success = false, message = "Silinecek adres bulunamadı veya yetkiniz yok." });
            }
        }

        private bool AdresExists(int id)
        {
            return _context.Adresler.Any(e => e.AdresId == id);
        }
    }
}