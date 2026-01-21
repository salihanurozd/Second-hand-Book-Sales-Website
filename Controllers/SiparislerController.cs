using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KitaplikApp.Data;
using KitaplikApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace KitaplikApp.Controllers
{
    [Authorize]
    public class SiparislerController : Controller
    {
        private readonly KitaplikDbContext _context;

        public SiparislerController(KitaplikDbContext context)
        {
            _context = context;
        }

        // -- Yardımcı Metotlar --
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        private string? GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }

        //CRUD

        // GET: Siparisler (Siparişleri role göre listele)
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var userRole = GetCurrentUserRole();
            IQueryable<Siparisler> siparislerQuery;

            if (userRole == "Admin")
            {
                // Admin tüm siparişleri görür
                siparislerQuery = _context.Siparisler
                    .Include(s => s.AliciKullanici)
                    .Include(s => s.TeslimatAdres)
                    .Include(s => s.SiparisDetaylaris!)
                        .ThenInclude(sd => sd.Kitap!)
                            .ThenInclude(k => k.SaticiKullanici)
                    .OrderByDescending(s => s.SiparisTarihi);
            }
            else // Diğer tüm roller (Kullanici ve Satici dahil) kendi siparişlerini görür
            {
                siparislerQuery = _context.Siparisler
                    .Include(s => s.AliciKullanici)
                    .Include(s => s.TeslimatAdres)
                    .Include(s => s.SiparisDetaylaris!)
                        .ThenInclude(sd => sd.Kitap!)
                    .Where(s => s.AliciKullaniciId == userId.Value) // AliciKullaniciId'ye göre filtrele
                    .OrderByDescending(s => s.SiparisTarihi);
            }

            return View(await siparislerQuery.ToListAsync());
        }
        // GET: Siparisler/Details (Sipariş detaylarını modal'da göster)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            if (!userId.HasValue || string.IsNullOrEmpty(userRole)) return Forbid();

            var siparis = await _context.Siparisler
                .Include(s => s.AliciKullanici)
                .Include(s => s.TeslimatAdres)
                .Include(s => s.SiparisDetaylaris!)
                    .ThenInclude(sd => sd.Kitap!)
                        .ThenInclude(k => k.SaticiKullanici)
                .Include(s => s.Fatura) // Fatura bilgilerini de getir
                .FirstOrDefaultAsync(m => m.SiparisId == id);

            if (siparis == null) return NotFound();

            // Rol bazlı yetkilendirme 
            if (userRole == "Admin")
            {
                return PartialView("Details", siparis);
            }

            if (userRole == "Kullanici" && siparis.AliciKullaniciId == userId.Value)
            {
                return PartialView("Details", siparis);
            }

            if (userRole == "Satici")
            {
                bool isSellerOfOrder = siparis.SiparisDetaylaris!.Any(sd => sd.Kitap!.SaticiKullaniciId == userId.Value);
                bool isBuyerOfOrder = siparis.AliciKullaniciId == userId.Value;

                if (isSellerOfOrder || isBuyerOfOrder)
                {
                    return PartialView("Details", siparis);
                }
            }

            return Forbid();
        }

        // GET: Siparisler/Checkout
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Kullanicilar");
            }
            var sepet = await _context.Sepetler
                .Include(s => s.SepetDetaylari!)
                    .ThenInclude(sd => sd.Kitap!)
                        .ThenInclude(k => k.KitapGorselleri)
                .FirstOrDefaultAsync(s => s.KullaniciId == userId.Value);

            if (sepet == null || sepet.SepetDetaylari == null || !sepet.SepetDetaylari.Any())
            {
                TempData["ErrorMessage"] = "Sepetiniz boş. Lütfen önce sepetinize ürün ekleyin.";
                return RedirectToAction("Index", "Sepet");
            }

            var adresler = await _context.Adresler
                .Where(a => a.KullaniciId == userId.Value)
                .ToListAsync();

            ViewBag.Sepet = sepet;
            ViewData["TeslimatAdresId"] = new SelectList(adresler, "AdresId", "AdresBasligi");

            return View(new Siparisler());
        }

        // GET: Siparisler/UpdateStatus (Sipariş durumunu güncelleme formu modal'da gösterir)
        [Authorize(Roles = "Admin,Satici")]
        public async Task<IActionResult> UpdateStatus(int? id)
        {
            if (id == null) return NotFound();

            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            if (!userId.HasValue || string.IsNullOrEmpty(userRole)) return Forbid();

            var siparis = await _context.Siparisler
                .Include(s => s.SiparisDetaylaris!)
                    .ThenInclude(sd => sd.Kitap!)
                        .ThenInclude(k => k.SaticiKullanici)
                .FirstOrDefaultAsync(s => s.SiparisId == id);

            if (siparis == null) return NotFound();

            //Satıcı sadece kendi siparişlerini güncelleyebilir
            if (userRole == "Satici" && !siparis.SiparisDetaylaris!.Any(sd => sd.Kitap!.SaticiKullaniciId == userId.Value))
            {
                return Forbid();
            }

            ViewBag.SiparisDurumlari = new SelectList(new List<string> { "Beklemede", "Hazırlanıyor", "Kargoda", "Teslim Edildi", "İptal Edildi" }, siparis.SiparisDurumu);
            return PartialView("UpdateStatus", siparis);
        }

        // POST: Siparisler/UpdateStatus (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Satici")]
        public async Task<IActionResult> UpdateStatusConfirmed(int id, [Bind("SiparisDurumu")] Siparisler siparisModel)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            if (!userId.HasValue || string.IsNullOrEmpty(userRole)) return Forbid();

            var siparisToUpdate = await _context.Siparisler
                .Include(s => s.SiparisDetaylaris!)
                    .ThenInclude(sd => sd.Kitap!)
                .FirstOrDefaultAsync(s => s.SiparisId == id);

            if (siparisToUpdate == null) return Json(new { success = false, message = "Güncellenecek sipariş bulunamadı." });

            if (userRole == "Satici" && !siparisToUpdate.SiparisDetaylaris!.Any(sd => sd.Kitap!.SaticiKullaniciId == userId.Value))
            {
                return Json(new { success = false, message = "Bu siparişi güncelleme yetkiniz yoktur." });
            }
            
            // Sipariş iptal edildiğinde stokları geri ekle
            if (siparisToUpdate.SiparisDurumu != "İptal Edildi" && siparisModel.SiparisDurumu == "İptal Edildi")
            {
                foreach (var siparisDetay in siparisToUpdate.SiparisDetaylaris!)
                {
                    var kitap = await _context.Kitaplar.FindAsync(siparisDetay.KitapId);
                    if (kitap != null)
                    {
                        kitap.Stok += siparisDetay.Adet;
                        _context.Update(kitap);
                    }
                }
            }

            siparisToUpdate.SiparisDurumu = siparisModel.SiparisDurumu;

            _context.Update(siparisToUpdate);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Sipariş durumu başarıyla güncellendi." });
        }

        //  GELEN SİPARİŞLER (SATICI PANELİ) 
        [Authorize(Roles = "Satici")]
        public async Task<IActionResult> GelenSiparisler()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "Kullanıcı bilgisi alınamadı.";
                return RedirectToAction("Login", "Account");
            }
            
            var gelenSiparisler = await _context.Siparisler
                .Include(s => s.AliciKullanici)
                .Include(s => s.TeslimatAdres)
                .Include(s => s.SiparisDetaylaris!)
                    .ThenInclude(sd => sd.Kitap!)
                        .ThenInclude(k => k.SaticiKullanici)
                .Where(s => s.SiparisDetaylaris!.Any(sd => sd.Kitap!.SaticiKullaniciId == userId.Value))
                .OrderByDescending(s => s.SiparisTarihi)
                .ToListAsync();

            return View("GelenSiparisler", gelenSiparisler);
        }

        // POST: Siparisler/Delete/5 (AJAX)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var siparis = await _context.Siparisler.Include(s => s.SiparisDetaylaris).FirstOrDefaultAsync(s => s.SiparisId == id);
            if (siparis == null)
            {
                return Json(new { success = false, message = "Silinecek sipariş bulunamadı." });
            }

            try
            {
                _context.SiparisDetaylari.RemoveRange(siparis.SiparisDetaylaris!);
                _context.Siparisler.Remove(siparis);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Sipariş başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Sipariş silinirken bir hata oluştu: {ex.Message}" });
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder([Bind("TeslimatAdresId")] Siparisler siparisModel)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            // Kullanıcının sepetini al
            var sepet = await _context.Sepetler
                .Include(s => s.SepetDetaylari!)
                    .ThenInclude(sd => sd.Kitap)
                .FirstOrDefaultAsync(s => s.KullaniciId == userId.Value);

            if (sepet == null || !sepet.SepetDetaylari!.Any())
            {
                TempData["ErrorMessage"] = "Sepetinizde ürün bulunmamaktadır.";
                return RedirectToAction("Index", "Sepet");
            }
            
            // Siparişten önce stok kontrolü ve stoğu düşürme
            foreach (var sepetDetayi in sepet.SepetDetaylari!)
            {
                var kitap = sepetDetayi.Kitap;
                if (kitap == null)
                {
                    TempData["ErrorMessage"] = "Sepetteki bir ürün bulunamadı. Lütfen tekrar deneyin.";
                    return RedirectToAction("Index", "Sepetler");
                }
                
                // Stok kontrolü yap
                if (kitap.Stok < sepetDetayi.Miktar)
                {
                    TempData["ErrorMessage"] = $"Üzgünüz, '{kitap.Baslik}' için yeterli stok kalmadı. Sipariş oluşturulamadı.";
                    return RedirectToAction("Index", "Sepetler");
                }
                
                // Stoğu düşür
                kitap.Stok -= sepetDetayi.Miktar;
                _context.Update(kitap);
            }

            if (siparisModel.TeslimatAdresId == 0)
            {
                TempData["ErrorMessage"] = "Lütfen bir teslimat adresi seçiniz.";
                ViewBag.Sepet = sepet;
                var adresler = await _context.Adresler.Where(a => a.KullaniciId == userId.Value).ToListAsync();
                ViewData["TeslimatAdresId"] = new SelectList(adresler, "AdresId", "AdresBasligi");
                return View("Checkout", siparisModel);
            }

            // Yeni bir sipariş nesnesi oluştur
            var yeniSiparis = new Siparisler
            {
                AliciKullaniciId = userId.Value,
                TeslimatAdresId = siparisModel.TeslimatAdresId,
                SiparisTarihi = DateTime.Now,
                SiparisDurumu = "Hazırlanıyor",
                ToplamTutar = sepet.SepetDetaylari.Sum(sd => sd.Miktar * sd.BirimFiyat)
            };

            _context.Add(yeniSiparis);
            await _context.SaveChangesAsync();
            
            // Sipariş oluştuktan sonra faturayı oluştur
            var yeniFatura = new Faturalar
            {
                SiparisId = yeniSiparis.SiparisId,
                FaturaTarihi = DateTime.Now
            };
            _context.Add(yeniFatura);


            // Sepet Detaylarını Sipariş Detaylarına dönüştür
            foreach (var sepetDetay in sepet.SepetDetaylari)
            {
                var siparisDetay = new SiparisDetaylari
                {
                    SiparisId = yeniSiparis.SiparisId,
                    KitapId = sepetDetay.KitapId,
                    Adet = sepetDetay.Miktar,
                    BirimFiyat = sepetDetay.BirimFiyat
                };
                _context.SiparisDetaylari.Add(siparisDetay);
            }

            // Sepeti temizle
            _context.SepetDetaylari.RemoveRange(sepet.SepetDetaylari);
            _context.Sepetler.Remove(sepet);
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Siparişiniz başarıyla oluşturuldu. Sipariş No: {yeniSiparis.SiparisId}";
            return RedirectToAction("Index", "Siparisler");
        }
    }
}
