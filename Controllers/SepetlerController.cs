using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using KitaplikApp.Data;
using KitaplikApp.Models;

namespace KitaplikApp.Controllers
{
    [Authorize]
    public class SepetlerController : Controller
    {
        private readonly KitaplikDbContext _context;

        public SepetlerController(KitaplikDbContext context)
        {
            _context = context;
        }

        // GET: Sepet (Kullanıcının sepetini göster)
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int parsedUserId))
            {
                TempData["ErrorMessage"] = "Sepetinizi görüntülemek için giriş yapmalısınız.";
                return RedirectToAction("Login", "Account");
            }

            var sepet = await _context.Sepetler
                                     .Include(s => s.SepetDetaylari)
                                         .ThenInclude(sd => sd.Kitap)
                                             .ThenInclude(k => k.KitapGorselleri)
                                     .FirstOrDefaultAsync(s => s.KullaniciId == parsedUserId);

            if (sepet == null)
            {
                sepet = new Sepetler { KullaniciId = parsedUserId, OlusturmaTarihi = DateTime.Now, SonGuncellemeTarihi = DateTime.Now };
            }

            return View(sepet);
        }

        // POST: Sepet/AddItem 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int kitapId, int miktar = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int parsedUserId))
            {
                return Json(new { success = false, message = "Sepete eklemek için giriş yapmalısınız." });
            }

            var kitap = await _context.Kitaplar.Include(k => k.SaticiKullanici).FirstOrDefaultAsync(k => k.KitapId == kitapId);
            if (kitap == null)
            {
                return Json(new { success = false, message = "Kitap bulunamadı." });
            }

            if (kitap.SaticiKullaniciId == parsedUserId)
            {
                return Json(new { success = false, message = "Kendi kitabınızı sepete ekleyemezsiniz." });
            }

            if (miktar <= 0)
            {
                return Json(new { success = false, message = "Miktar pozitif bir sayı olmalıdır." });
            }

            var sepet = await _context.Sepetler
                                        .Include(s => s.SepetDetaylari)
                                        .FirstOrDefaultAsync(s => s.KullaniciId == parsedUserId);

            if (sepet == null)
            {
                sepet = new Sepetler { KullaniciId = parsedUserId, OlusturmaTarihi = DateTime.Now, SonGuncellemeTarihi = DateTime.Now };
                _context.Sepetler.Add(sepet);
                await _context.SaveChangesAsync();
            }

            var sepetDetayi = sepet.SepetDetaylari.FirstOrDefault(sd => sd.KitapId == kitapId);
            int yeniMiktar = miktar;

            if (sepetDetayi != null)
            {
                yeniMiktar = sepetDetayi.Miktar + miktar;
            }

            // Doğru stok kontrolü: Sepete eklenen miktar, mevcut stoktan fazla olamaz.
            if (kitap.Stok < yeniMiktar)
            {
                return Json(new { success = false, message = $"Üzgünüz, bu ürün için yeterli stok bulunmamaktadır. Stok: {kitap.Stok}" });
            }

            if (sepetDetayi == null)
            {
                sepetDetayi = new SepetDetaylari
                {
                    SepetId = sepet.SepetId,
                    KitapId = kitapId,
                    Miktar = miktar,
                    BirimFiyat = kitap.Fiyat
                };
                _context.SepetDetaylari.Add(sepetDetayi);
            }
            else
            {
                sepetDetayi.Miktar = yeniMiktar;
                _context.SepetDetaylari.Update(sepetDetayi);
            }
           
            // Stok, sadece sipariş oluşturulduğunda düşürülmelidir

            sepet.SonGuncellemeTarihi = DateTime.Now;
            _context.Sepetler.Update(sepet);

            await _context.SaveChangesAsync();

            var updatedCartItemCount = await _context.Sepetler
                                                    .Where(s => s.KullaniciId == parsedUserId)
                                                    .SelectMany(s => s.SepetDetaylari)
                                                    .SumAsync(sd => sd.Miktar);

            return Json(new { success = true, message = $"{kitap.Baslik} sepete eklendi!", cartItemCount = updatedCartItemCount });
        }

        // POST: Sepet/RemoveFromCart 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int sepetDetayId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int parsedUserId))
            {
                return Json(new { success = false, message = "Sepetten kaldırmak için giriş yapmalısınız." });
            }

            var sepetDetayi = await _context.SepetDetaylari
                                        .Include(sd => sd.Sepet)
                                        .Include(sd => sd.Kitap)
                                        .FirstOrDefaultAsync(sd => sd.SepetDetayId == sepetDetayId && sd.Sepet.KullaniciId == parsedUserId);

            if (sepetDetayi == null)
            {
                return Json(new { success = false, message = "Sepet öğesi bulunamadı veya yetkiniz yok." });
            }

            // Stok zaten sepete eklenirken düşürülmediği için geri artırılmasına gerek yok.

            _context.SepetDetaylari.Remove(sepetDetayi);
            await _context.SaveChangesAsync();

            var updatedCartItemCount = await _context.Sepetler
                                                    .Where(s => s.KullaniciId == parsedUserId)
                                                    .SelectMany(s => s.SepetDetaylari)
                                                    .SumAsync(sd => sd.Miktar);

            return Json(new { success = true, message = "Ürün sepetten kaldırıldı.", cartItemCount = updatedCartItemCount });
        }

        // POST: Sepet/UpdateCartItemQuantity 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCartItemQuantity(int sepetDetayId, int newQuantity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int parsedUserId))
            {
                return Json(new { success = false, message = "Miktarı güncellemek için giriş yapmalısınız." });
            }

            var sepetDetayi = await _context.SepetDetaylari
                                           .Include(sd => sd.Sepet)
                                           .Include(sd => sd.Kitap)
                                           .FirstOrDefaultAsync(sd => sd.SepetDetayId == sepetDetayId && sd.Sepet.KullaniciId == parsedUserId);

            if (sepetDetayi == null)
            {
                return Json(new { success = false, message = "Sepet öğesi bulunamadı veya yetkiniz yok." });
            }

            // Doğru stok kontrolü: Sepetteki yeni miktar, mevcut stoktan fazla olamaz.
            if (newQuantity > sepetDetayi.Kitap.Stok)
            {
                 return Json(new { success = false, message = $"Maksimum {sepetDetayi.Kitap.Stok} adet ürün ekleyebilirsiniz." });
            }

            decimal updatedCartTotalPrice;
            int updatedCartItemCount;
            string successMessage;
            
            if (newQuantity <= 0)
            {
                _context.SepetDetaylari.Remove(sepetDetayi);
                successMessage = "Ürün sepetten kaldırıldı.";
            }
            else
            {            
                // Stok, sadece sipariş tamamlandığında düşürülmelidir.
                sepetDetayi.Miktar = newQuantity;
                sepetDetayi.BirimFiyat = sepetDetayi.Kitap.Fiyat;
                _context.SepetDetaylari.Update(sepetDetayi);
                successMessage = "Ürün miktarı güncellendi.";
            }

            await _context.SaveChangesAsync();

            var updatedSepet = await _context.Sepetler.Include(s => s.SepetDetaylari).ThenInclude(sd => sd.Kitap).FirstOrDefaultAsync(s => s.KullaniciId == parsedUserId);

            updatedCartTotalPrice = updatedSepet?.SepetDetaylari?.Sum(sd => sd.Miktar * sd.BirimFiyat) ?? 0;
            updatedCartItemCount = updatedSepet?.SepetDetaylari?.Sum(sd => sd.Miktar) ?? 0;

            decimal itemTotalPrice = sepetDetayi.Miktar * sepetDetayi.BirimFiyat;

            return Json(new { success = true, message = successMessage, itemTotalPrice = itemTotalPrice.ToString("F2"), cartTotalPrice = updatedCartTotalPrice.ToString("F2"), cartItemCount = updatedCartItemCount });
        }
        
        // Ana sayfadaki sepet sayısı
        [HttpGet]
        public async Task<IActionResult> GetCartItemCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int parsedUserId))
            {
                return Json(new { success = false, count = 0 });
            }

            var sepet = await _context.Sepetler
                                     .Include(s => s.SepetDetaylari)
                                     .FirstOrDefaultAsync(s => s.KullaniciId == parsedUserId);
            
            var count = sepet?.SepetDetaylari?.Sum(sd => sd.Miktar) ?? 0;

            return Json(new { success = true, count = count });
        }
    }
}
