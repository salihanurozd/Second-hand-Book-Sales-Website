using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KitaplikApp.Data;
using KitaplikApp.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using KitaplikApp.ViewModels;

namespace KitaplikApp.Controllers
{
    [Authorize]
    public class KitaplarController : Controller
    {
        private readonly KitaplikDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public KitaplarController(KitaplikDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // ADMİN PANELİ KİTAP LİSTESİ
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            ViewBag.IsAdminPage = true; // Admin sayfası olduğunu belirt
            var kitaplarQuery = _context.Kitaplar
                                        .Include(k => k.Yazar)
                                        .Include(k => k.Yayinevi)
                                        .Include(k => k.Kategori)
                                        .Include(k => k.KitapDurumu)
                                        .Include(k => k.SaticiKullanici)
                                        .Include(k => k.KitapGorselleri)
                                        .AsQueryable();

            return View(await kitaplarQuery.ToListAsync());
        }

        //  ANA SAYFA KİTAP LİSTESİ (ARAMA VE FİLTRELEME) 
        [AllowAnonymous]
        public async Task<IActionResult> MainIndex(string aramaKelimesi, int? secilenKategoriId, int? secilenYazarId, decimal? minFiyat, decimal? maxFiyat, string durum)
        {
            ViewBag.IsAdminPage = false;
            var kitaplarQuery = _context.Kitaplar
                                        .Include(k => k.Yazar)
                                        .Include(k => k.Kategori)
                                        .Include(k => k.KitapGorselleri)
                                        .Where(k => k.OnayDurumu == "Onaylandı")
                                        .AsQueryable();

            // Filtreleme ve arama 
            if (!string.IsNullOrEmpty(aramaKelimesi))
            {
                kitaplarQuery = kitaplarQuery.Where(k => 
                    k.Baslik.Contains(aramaKelimesi) ||
                    k.Yazar.YazarAdi.Contains(aramaKelimesi) ||
                    k.Aciklama.Contains(aramaKelimesi)
                );
            }

            if (secilenKategoriId.HasValue)
            {
                kitaplarQuery = kitaplarQuery.Where(k => k.KategoriId == secilenKategoriId.Value);
            }

            if (secilenYazarId.HasValue)
            {
                kitaplarQuery = kitaplarQuery.Where(k => k.YazarId == secilenYazarId.Value);
            }
            
            if (minFiyat.HasValue)
            {
                kitaplarQuery = kitaplarQuery.Where(k => k.Fiyat >= minFiyat.Value);
            }
            
            if (maxFiyat.HasValue)
            {
                kitaplarQuery = kitaplarQuery.Where(k => k.Fiyat <= maxFiyat.Value);
            }

            // ViewModel
            var viewModel = new KitapListelemeViewModel
            {
                Kitaplar = await kitaplarQuery.ToListAsync(),
                Kategoriler = new SelectList(await _context.Kategoriler.ToListAsync(), "KategoriId", "KategoriAdi", secilenKategoriId),
                Yazarlar = new SelectList(await _context.Yazarlar.ToListAsync(), "YazarId", "YazarAdi", secilenYazarId),

                AramaKelimesi = aramaKelimesi,
                SecilenKategoriId = secilenKategoriId,
                SecilenYazarId = secilenYazarId,
                MinFiyat = minFiyat,
                MaxFiyat = maxFiyat,
            };
            return View("MainIndex", viewModel);
        }
        // DETAILS  
        [Authorize(Roles = "Admin,Satici,Kullanici")] 
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                // AJAX isteği değilse NotFound döndür
                if (Request.Headers["X-Requested-With"] != "XMLHttpRequest")
                    return NotFound();

                // AJAX isteğiyse hata mesajı ile JSON döndür
                return Json(new { success = false, message = "Kitap kimliği belirtilmedi." });
            }

            var kitap = await _context.Kitaplar
                .Include(k => k.Yazar)
                .Include(k => k.Yayinevi)
                .Include(k => k.Kategori)
                .Include(k => k.KitapDurumu)
                .Include(k => k.SaticiKullanici)
                .Include(k => k.KitapGorselleri)
                .Include(k => k.Yorumlars)
                    .ThenInclude(y => y.Kullanici)
                .FirstOrDefaultAsync(m => m.KitapId == id);

            if (kitap == null)
            {
                if (Request.Headers["X-Requested-With"] != "XMLHttpRequest")
                    return NotFound();

                return Json(new { success = false, message = "Kitap bulunamadı." });
            }
            
            //AJAX isteği mi kontrol et
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(kitap);
            }
            else
            {
                return View(kitap);
            }
        }
        
        //SATICININ KİTAPLARINI LİSTELEME
        [Authorize(Roles = "Satici")]
        public async Task<IActionResult> MyBooks()
        {
            var saticiIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (saticiIdClaim == null || !int.TryParse(saticiIdClaim.Value, out int saticiKullaniciId))
            {
                TempData["ErrorMessage"] = "Kullanıcı kimliği alınamadı, lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Account");
            }
            
            var saticiKitaplari = await _context.Kitaplar
                                                .Include(k => k.Kategori)
                                                .Include(k => k.KitapDurumu)
                                                .Include(k => k.KitapGorselleri)
                                                .Where(k => k.SaticiKullaniciId == saticiKullaniciId)
                                                .ToListAsync();

            return View(saticiKitaplari);
        }

        //UPSERT (GET)
        [Authorize(Roles = "Admin,Satici")]
        public async Task<IActionResult> Upsert(int? id)
        {
            var viewModel = new KitapUpsertViewModel();
            Kitaplar kitap = null;

            if (id == null || id == 0)
            {
                kitap = new Kitaplar
                {
                    YayinlanmaTarihi = DateTime.Now,
                    SonGuncellenmeTarihi = DateTime.Now,
                    OnayDurumu = "Beklemede",
                    Stok = 0 // Yeni eklenen bir kitap için başlangıç stok değeri
                };

                if (User.IsInRole("Satici") && !User.IsInRole("Admin"))
                {
                    var saticiIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (saticiIdClaim != null && int.TryParse(saticiIdClaim.Value, out int saticiKullaniciId))
                    {
                        kitap.SaticiKullaniciId = saticiKullaniciId;
                    }
                }
            }
            else
            {
                kitap = await _context.Kitaplar
                    .Include(k => k.SaticiKullanici)
                    .FirstOrDefaultAsync(m => m.KitapId == id);
                
                if (kitap == null)
                {
                    return NotFound();
                }

                if (User.IsInRole("Satici") && !User.IsInRole("Admin"))
                {
                    var saticiIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (saticiIdClaim != null && int.TryParse(saticiIdClaim.Value, out int saticiKullaniciId))
                    {
                        if (kitap.SaticiKullaniciId != saticiKullaniciId)
                        {
                            TempData["ErrorMessage"] = "Bu kitaba erişim izniniz yok.";
                            return RedirectToAction(nameof(MyBooks));
                        }
                    }
                }
            }
            
            PopulateViewModelDropdowns(viewModel, kitap);
            viewModel.Kitap = kitap;
            return PartialView(viewModel);
        }

        //UPSERT (POST) 
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Satici")]
        public async Task<IActionResult> Upsert(KitapUpsertViewModel viewModel, IFormFile? resimDosyasi)
        {
            // YENİ EKLENEN KOD: Stok bilgisini doğrulamadan çıkarıyoruz
            ModelState.Remove(nameof(KitapUpsertViewModel.Kitap.Stok));
            // ... Mevcut kodunuzdaki diğer ModelState.Remove satırları ...
            ModelState.Remove(nameof(KitapUpsertViewModel.Kitap));
            ModelState.Remove(nameof(KitapUpsertViewModel.Kategoriler));
            ModelState.Remove(nameof(KitapUpsertViewModel.Yazarlar));
            ModelState.Remove(nameof(KitapUpsertViewModel.Yayinevleri));
            ModelState.Remove(nameof(KitapUpsertViewModel.KitapDurumlari));
            ModelState.Remove(nameof(KitapUpsertViewModel.Saticilar));
            ModelState.Remove(nameof(Kitaplar.KitapGorselleri));
            ModelState.Remove(nameof(Kitaplar.Yorumlars));
            ModelState.Remove(nameof(Kitaplar.SaticiKullanici));
            ModelState.Remove(nameof(Kitaplar.Yazar));
            ModelState.Remove(nameof(Kitaplar.Yayinevi));
            ModelState.Remove(nameof(Kitaplar.Kategori));
            ModelState.Remove(nameof(Kitaplar.KitapDurumu));

            var saticiIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int parsedCurrentUserId = 0;
            if (saticiIdClaim == null || !int.TryParse(saticiIdClaim.Value, out parsedCurrentUserId))
            {
                ModelState.AddModelError(string.Empty, "Kullanıcı kimliği alınamadı.");
            }

            if (ModelState.IsValid)
            {
                if (resimDosyasi != null)
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string fileName = Path.GetFileName(resimDosyasi.FileName); 
                    string path = Path.Combine(wwwRootPath, @"images\kitaplar");
                    string filePath = Path.Combine(path, fileName);

                    if (viewModel.Kitap.KitapId != 0)
                    {
                        var oldImage = _context.KitapGorselleri.FirstOrDefault(g => g.KitapId == viewModel.Kitap.KitapId);
                        if (oldImage != null)
                        {
                            var oldImagePath = Path.Combine(wwwRootPath, oldImage.ResimUrl.TrimStart('\\'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                            _context.KitapGorselleri.Remove(oldImage);
                        }
                    }
                    
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await resimDosyasi.CopyToAsync(fileStream);
                    }

                    var kitapGorseli = new KitapGorselleri
                    {
                        ResimUrl = @"\images\kitaplar\" + fileName,
                        KitapId = viewModel.Kitap.KitapId
                    };
                    
                    if(viewModel.Kitap.KitapId == 0)
                    {
                        if (viewModel.Kitap.KitapGorselleri == null)
                            viewModel.Kitap.KitapGorselleri = new List<KitapGorselleri>();
                        
                        viewModel.Kitap.KitapGorselleri.Add(kitapGorseli);
                    }
                    else
                    {
                        kitapGorseli.KitapId = viewModel.Kitap.KitapId;
                        _context.KitapGorselleri.Add(kitapGorseli);
                    }
                }
                
                if (viewModel.Kitap.KitapId == 0)
                {
                    if (User.IsInRole("Satici") && !User.IsInRole("Admin"))
                    {
                        viewModel.Kitap.SaticiKullaniciId = parsedCurrentUserId;
                    }
                    viewModel.Kitap.YayinlanmaTarihi = DateTime.Now;
                    viewModel.Kitap.SonGuncellenmeTarihi = DateTime.Now;
                    viewModel.Kitap.OnayDurumu = "Beklemede";
                    
                    _context.Add(viewModel.Kitap);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Kitap başarıyla eklendi ve onay bekliyor." });
                }
                else
                {
                    var existingBook = await _context.Kitaplar.AsNoTracking().FirstOrDefaultAsync(k => k.KitapId == viewModel.Kitap.KitapId);
                    if (existingBook == null)
                    {
                        return Json(new { success = false, message = "Kitap bulunamadı." });
                    }
                    
                    if (User.IsInRole("Satici") && !User.IsInRole("Admin"))
                    {
                        if (existingBook.SaticiKullaniciId != parsedCurrentUserId)
                        {
                            return Json(new { success = false, message = "Bu kitabı düzenleme yetkiniz yok." });
                        }
                    }

                    try
                    {
                        viewModel.Kitap.YayinlanmaTarihi = existingBook.YayinlanmaTarihi;
                        viewModel.Kitap.SonGuncellenmeTarihi = DateTime.Now;
                        
                        if (!User.IsInRole("Admin"))
                        {
                            viewModel.Kitap.SaticiKullaniciId = existingBook.SaticiKullaniciId;
                            viewModel.Kitap.OnayDurumu = existingBook.OnayDurumu;
                        }
                        
                        _context.Update(viewModel.Kitap);
                        await _context.SaveChangesAsync();
                        return Json(new { success = true, message = "Kitap bilgileri başarıyla güncellendi." });
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!KitaplarExists(viewModel.Kitap.KitapId))
                        {
                            return Json(new { success = false, message = "Kitap veritabanında bulunamadı." });
                        }
                        throw;
                    }
                }
            }
            
            PopulateViewModelDropdowns(viewModel, viewModel.Kitap);
            return PartialView(viewModel);
        }

        //Yorum Ekleme 
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> AddComment(int kitapId, int puan, string yorumMetni)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null || !int.TryParse(currentUserId, out int kullaniciId))
            {
                return Json(new { success = false, message = "Oturumunuzun süresi doldu, lütfen tekrar giriş yapın." });
            }

            if (string.IsNullOrWhiteSpace(yorumMetni) || puan < 1 || puan > 5)
            {
                return Json(new { success = false, message = "Lütfen yorum ve puan alanlarını doğru şekilde doldurun." });
            }

            // Yorum oluştur
            var yorum = new Yorumlar
            {
                KitapId = kitapId,
                KullaniciId = kullaniciId,
                Puan = puan,
                YorumMetni = yorumMetni,
                YorumTarihi = DateTime.Now
            };

            try
            {
                _context.Yorumlar.Add(yorum);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Yorumunuz başarıyla eklendi." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Yorum eklenirken beklenmeyen bir hata oluştu." });
            }
        }
                
        // DELETE (GET) 
        [Authorize(Roles = "Admin,Satici")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var kitap = await _context.Kitaplar
                .Include(k => k.SaticiKullanici)
                .FirstOrDefaultAsync(m => m.KitapId == id);
            
            if (kitap == null) return NotFound();

            if (User.IsInRole("Satici") && !User.IsInRole("Admin"))
            {
                var saticiIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (saticiIdClaim != null && int.TryParse(saticiIdClaim.Value, out int saticiKullaniciId))
                {
                    if (kitap.SaticiKullaniciId != saticiKullaniciId)
                    {
                        TempData["ErrorMessage"] = "Bu kitabı silme yetkiniz yok.";
                        return RedirectToAction(nameof(MyBooks));
                    }
                }
            }
            return PartialView(kitap);
        }

        //DELETE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Satici")]
        public async Task<IActionResult> Delete(int id)
        {
            var kitap = await _context.Kitaplar
                .Include(k => k.KitapGorselleri)
                .FirstOrDefaultAsync(k => k.KitapId == id);
            
            if (kitap == null)
            {
                return Json(new { success = false, message = "Silinecek kitap bulunamadı." });
            }

            if (User.IsInRole("Satici") && !User.IsInRole("Admin"))
            {
                var saticiIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (saticiIdClaim != null && int.TryParse(saticiIdClaim.Value, out int saticiKullaniciId))
                {
                    if (kitap.SaticiKullaniciId != saticiKullaniciId)
                    {
                        return Json(new { success = false, message = "Bu kitabı silme yetkiniz yok." });
                    }
                }
            }
            
            try
            {
                foreach (var gorsel in kitap.KitapGorselleri)
                {
                    var imagePath = Path.Combine(_hostEnvironment.WebRootPath, gorsel.ResimUrl.TrimStart('\\'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Kitaplar.Remove(kitap);
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, message = "Kitap başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Kitap silinirken bir hata oluştu: {ex.Message}" });
            }
        } //

        private bool KitaplarExists(int id)
        {
            return _context.Kitaplar.Any(e => e.KitapId == id);
        }

        private void PopulateViewModelDropdowns(KitapUpsertViewModel viewModel, Kitaplar? kitap)
        {
            var modelToUse = kitap ?? new Kitaplar();

            viewModel.Kategoriler = new SelectList(_context.Kategoriler, "KategoriId", "KategoriAdi", modelToUse.KategoriId);
            viewModel.KitapDurumlari = new SelectList(_context.KitapDurumlari, "DurumId", "DurumAdi", modelToUse.KitapDurumuId);
            viewModel.Yazarlar = new SelectList(_context.Yazarlar, "YazarId", "YazarAdi", modelToUse.YazarId);
            viewModel.Yayinevleri = new SelectList(_context.Yayinevleri, "YayineviId", "YayineviAdi", modelToUse.YayineviId);

            if (User.IsInRole("Admin"))
            {
                viewModel.Saticilar = new SelectList(_context.Kullanicilar.Include(u => u.Rol).Where(u => u.Rol != null && (u.Rol.RolAdi == "Satici" || u.Rol.RolAdi == "Admin")), "KullaniciId", "Eposta", modelToUse.SaticiKullaniciId);
            }
        }
    }
}