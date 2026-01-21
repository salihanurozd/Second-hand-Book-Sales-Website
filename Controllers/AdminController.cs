using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using KitaplikApp.Data;
using KitaplikApp.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using BCrypt.Net;
using System;

namespace KitaplikApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly KitaplikDbContext _context;

        public AdminController(KitaplikDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Index
        public async Task<IActionResult> Index() 
        {
            var users = await _context.Kullanicilar.Include(k => k.Rol).ToListAsync();
            return View(users);
        }

        // GET: Admin/Details modal içinde
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var kullanici = await _context.Kullanicilar
                .Include(k => k.Rol)
                .Include(k => k.Adresler)
                .FirstOrDefaultAsync(m => m.KullaniciId == id);

            if (kullanici == null) return NotFound();
            return PartialView("Details", kullanici);
        }

        // GET: Admin/EditUser -modalı göster
        [HttpGet]
        public async Task<IActionResult> EditUser(int? id)
        {
            Kullanicilar kullanici;
            ViewBag.Roles = await _context.Roller.ToListAsync();

            if (id == null || id == 0)
            {
                kullanici = new Kullanicilar();
            }
            else
            {
                kullanici = await _context.Kullanicilar.FirstOrDefaultAsync(k => k.KullaniciId == id);
                if (kullanici == null) return NotFound();
                kullanici.Sifre = null; 
            }
            return PartialView("EditUser", kullanici);
        }

        // POST: Admin/EditUser AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser([Bind("KullaniciId,Ad,Soyad,Eposta,Sifre,RolId,TelefonNumarasi")] Kullanicilar model)
        {
            if (model.KullaniciId != 0 && string.IsNullOrEmpty(model.Sifre))
            {
                ModelState.Remove("Sifre");
            }
            ModelState.Remove("Rol");
            ModelState.Remove("Adresler");
            ModelState.Remove("KayitTarihi");
            ModelState.Remove("SonGirisTarihi");
            
            if (ModelState.IsValid)
            {
                if (model.KullaniciId == 0) // Yeni kullanıcı oluşturma
                {
                    if (string.IsNullOrEmpty(model.Sifre))
                    {
                        ModelState.AddModelError("Sifre", "Şifre alanı boş bırakılamaz.");
                        ViewBag.Roles = await _context.Roller.ToListAsync();
                        return PartialView("EditUser", model);
                    }
                    model.Sifre = BCrypt.Net.BCrypt.HashPassword(model.Sifre, 12);
                    model.KayitTarihi = DateTime.Now;
                    model.SonGirisTarihi = null;
                    _context.Add(model);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Yeni kullanıcı başarıyla eklendi." });
                }
                else // Var olan kullanıcıyı düzenleme
                {
                    var existingUser = await _context.Kullanicilar.AsNoTracking().FirstOrDefaultAsync(u => u.KullaniciId == model.KullaniciId);
                    if (existingUser == null) return Json(new { success = false, message = "Kullanıcı bulunamadı." });

                    if (await _context.Kullanicilar.AnyAsync(u => u.Eposta == model.Eposta && u.KullaniciId != model.KullaniciId))
                    {
                        ModelState.AddModelError("Eposta", "Bu e-posta adresi zaten başka bir kullanıcı tarafından kullanılıyor.");
                        ViewBag.Roles = await _context.Roller.ToListAsync();
                        return PartialView("EditUser", model);
                    }
                    
                    model.Sifre = existingUser.Sifre;
                    model.KayitTarihi = existingUser.KayitTarihi;
                    model.SonGirisTarihi = existingUser.SonGirisTarihi;
                    
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Kullanıcı bilgileri başarıyla güncellendi." });
                }
            }

            ViewBag.Roles = await _context.Roller.ToListAsync();
            return PartialView("EditUser", model);
        }

        // GET: Admin/DeleteUser (Kullanıcı Silme Modalını göster)
        [HttpGet]
        public async Task<IActionResult> DeleteUser(int? id)
        {
            if (id == null) return NotFound();
            var kullanici = await _context.Kullanicilar.Include(k => k.Rol).FirstOrDefaultAsync(m => m.KullaniciId == id);
            if (kullanici == null) return NotFound();
            return PartialView("DeleteUser", kullanici);
        }

        // POST: Admin/DeleteUser (AJAX ile kullanıcı Silme işlemi)
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(int id)
        {
            try
            {
                var user = await _context.Kullanicilar.FindAsync(id);
                if (user != null)
                {
                    _context.Kullanicilar.Remove(user);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Kullanıcı başarıyla silindi." });
                }
                else
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı." });
                }
            }
            catch (DbUpdateException)
            {
                return Json(new { success = false, message = "Kullanıcı silinemedi. İlişkili kayıtları olabilir." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }
        
        // Admin için bekleyen satıcı başvurularını listeler
        public async Task<IActionResult> SellerApplications()
        {
            var pendingSellers = await _context.SaticiBasvurulari
                .Include(sa => sa.Kullanici)
                .ThenInclude(k => k.Rol)
                .ToListAsync();

            return View(pendingSellers);
        }

        // Admin için satıcı başvurusunu onaylar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveSeller(int id)
        {
            var application = await _context.SaticiBasvurulari.FirstOrDefaultAsync(sa => sa.KullaniciId == id);
            
            if (application == null)
            {
                TempData["ErrorMessage"] = "Başvuru bulunamadı veya zaten onaylanmış.";
                return RedirectToAction(nameof(SellerApplications));
            }

            var userToApprove = await _context.Kullanicilar.FindAsync(id);
            if(userToApprove == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction(nameof(SellerApplications));
            }

            var newSellerRole = await _context.Roller.FirstOrDefaultAsync(r => r.RolAdi == "Satici");

            if (newSellerRole == null)
            {
                TempData["ErrorMessage"] = "Roller bulunamadı. Lütfen 'Satici' rolünün mevcut olduğundan emin olun.";
                return RedirectToAction(nameof(SellerApplications));
            }

            userToApprove.RolId = newSellerRole.RolId;
            _context.Update(userToApprove);
            
            _context.SaticiBasvurulari.Remove(application);

            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"{userToApprove.Ad} {userToApprove.Soyad} kullanıcısı satıcı olarak onaylandı.";
            return RedirectToAction(nameof(SellerApplications));
        }

        // POST: Admin(Satıcı başvurusunu reddet)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectSeller(int id)
        {
            var basvuru = await _context.SaticiBasvurulari
                                        .FirstOrDefaultAsync(b => b.KullaniciId == id);
            
            if (basvuru == null)
            {
                TempData["ErrorMessage"] = "Reddedilecek başvuru bulunamadı.";
                return RedirectToAction("SellerApplications");
            }

            try
            {
                _context.SaticiBasvurulari.Remove(basvuru);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Satıcı başvurusu başarıyla reddedildi.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Başvuru reddedilirken bir hata oluştu: {ex.Message}";
            }

            return RedirectToAction("SellerApplications");
        }

        //diğer metotlar 
        private bool KullaniciExists(int id)
        {
            return _context.Kullanicilar.Any(e => e.KullaniciId == id);
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, 12);
        }
    }
}