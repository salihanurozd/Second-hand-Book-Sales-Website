using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KitaplikApp.Data;
using KitaplikApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using BCrypt.Net; 
using Microsoft.AspNetCore.Authorization; 

namespace KitaplikApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly KitaplikDbContext _context;

        public AccountController(KitaplikDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl; 
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string eposta, string sifre, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(eposta) || string.IsNullOrEmpty(sifre))
            {
                ModelState.AddModelError(string.Empty, "E-posta ve şifre boş bırakılamaz.");
                return View();
            }

            var user = await _context.Kullanicilar
                                    .Include(k => k.Rol)
                                    .FirstOrDefaultAsync(u => u.Eposta == eposta);

            if (user == null || !BCrypt.Net.BCrypt.Verify(sifre, user.Sifre))
            {
                ModelState.AddModelError(string.Empty, "Geçersiz e-posta veya şifre.");
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.KullaniciId.ToString()),
                new Claim(ClaimTypes.Email, user.Eposta),
                new Claim(ClaimTypes.GivenName, user.Ad),
                new Claim(ClaimTypes.Surname, user.Soyad)
            };

            if (user.Rol != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Rol.RolAdi));
            }

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            user.SonGirisTarihi = DateTime.Now;
            _context.Update(user);
            await _context.SaveChangesAsync();

            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Rol bazlı yönlendirme
            if (user.Rol != null && !string.IsNullOrEmpty(user.Rol.RolAdi))
            {
                switch (user.Rol.RolAdi)
                {
                    case "Admin":
                        return RedirectToAction("Index", "AdminPanel"); 
                    case "Satici":
                        return RedirectToAction("Index", "Kitaplar");
                    case "Kullanici":
                        return RedirectToAction("Index", "Home");
                    default:
                        return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
        // GET: /Account/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "Başarıyla çıkış yaptınız.";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View(new Kullanicilar());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("Eposta,Sifre,Ad,Soyad,TelefonNumarasi")] Kullanicilar kullanici, string SifreTekrar)
        {
            ModelState.Remove("Rol");
            ModelState.Remove("Adresler");
            ModelState.Remove("Yorumlars");
            ModelState.Remove("FavoriKitaplars");
            ModelState.Remove("Siparisler");
            ModelState.Remove("GonderilenMesajlars");
            ModelState.Remove("AlinanMesajlars");
            ModelState.Remove("SatisKitaplars");
            ModelState.Remove("KayitTarihi");
            ModelState.Remove("SonGirisTarihi");

            if (await _context.Kullanicilar.AnyAsync(u => u.Eposta == kullanici.Eposta))
            {
                ModelState.AddModelError("Eposta", "Bu e-posta adresi zaten kullanımda.");
            }

            if (string.IsNullOrEmpty(kullanici.Sifre))
            {
                ModelState.AddModelError("Sifre", "Şifre alanı boş bırakılamaz.");
            }
            else if (kullanici.Sifre != SifreTekrar)
            {
                ModelState.AddModelError("SifreTekrar", "Şifreler uyuşmuyor.");
            }

            if (ModelState.IsValid)
            {
                var defaultRole = await _context.Roller.FirstOrDefaultAsync(r => r.RolAdi == "Kullanici");
                if (defaultRole == null)
                {
                    ModelState.AddModelError(string.Empty, "Varsayılan 'Müşteri' rolü bulunamadı. Lütfen veritabanınızı kontrol edin.");
                    return View(kullanici);
                }

                kullanici.RolId = defaultRole.RolId;
                kullanici.Sifre = HashPassword(kullanici.Sifre);
                kullanici.KayitTarihi = DateTime.Now;
                kullanici.SonGirisTarihi = DateTime.Now;

                _context.Add(kullanici);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Kaydınız başarıyla oluşturuldu! Giriş yapabilirsiniz.";
                return RedirectToAction(nameof(Login));
            }

            TempData["ErrorMessage"] = "Kayıt olurken bir hata oluştu. Lütfen bilgileri kontrol edin.";
            return View(kullanici);
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: /Account/ManageProfile
        [HttpGet]
        [Authorize(Roles = "Kullanici, Satici, Admin")]
        public async Task<IActionResult> ManageProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                TempData["ErrorMessage"] = "Kullanıcı bilgisi alınamadı. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login");
            }

            var user = await _context.Kullanicilar
                                     .Include(k => k.Rol)
                                     .FirstOrDefaultAsync(u => u.KullaniciId == userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Index", "Home");
            }

            user.Sifre = null; 
            return View(user);
        }

        // POST: /Account/UpdateProfile 
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Kullanici, Satici, Admin")]
        public async Task<IActionResult> UpdateProfile([Bind("KullaniciId,Ad,Soyad,Eposta,TelefonNumarasi")] Kullanicilar model, string? newPassword = null, string? newPasswordConfirm = null)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                TempData["ErrorMessage"] = "Yetkisiz işlem. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login");
            }

            if (model.KullaniciId != currentUserId)
            {
                TempData["ErrorMessage"] = "Geçersiz kullanıcı ID'si.";
                return RedirectToAction("Index", "Home");
            }

            var userToUpdate = await _context.Kullanicilar.FindAsync(currentUserId);

            if (userToUpdate == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Index", "Home");
            }

            if (userToUpdate.Eposta != model.Eposta)
            {
                if (await _context.Kullanicilar.AnyAsync(u => u.Eposta == model.Eposta && u.KullaniciId != currentUserId))
                {
                    ModelState.AddModelError("Eposta", "Bu e-posta adresi zaten başka bir kullanıcı tarafından kullanılıyor.");
                }
            }

            if (!string.IsNullOrEmpty(newPassword))
            {
                if (newPassword != newPasswordConfirm)
                {
                    ModelState.AddModelError("newPasswordConfirm", "Yeni şifreler uyuşmuyor.");
                }
                if (newPassword.Length < 6)
                {
                    ModelState.AddModelError("newPassword", "Şifre en az 6 karakter olmalıdır.");
                }
            }
            else if (!string.IsNullOrEmpty(newPasswordConfirm))
            {
                 ModelState.AddModelError("newPassword", "Yeni şifre alanı boş bırakılamaz.");
            }

            if (!ModelState.IsValid)
            {
                userToUpdate.Sifre = null; 
                userToUpdate.Ad = model.Ad;
                userToUpdate.Soyad = model.Soyad;
                userToUpdate.Eposta = model.Eposta;
                userToUpdate.TelefonNumarasi = model.TelefonNumarasi;

                return View("ManageProfile", userToUpdate);
            }

            // Verileri güncelle
            userToUpdate.Ad = model.Ad;
            userToUpdate.Soyad = model.Soyad;
            userToUpdate.Eposta = model.Eposta;
            userToUpdate.TelefonNumarasi = model.TelefonNumarasi;

            // yeni şifre girildiyse şifreyi güncelle
            if (!string.IsNullOrEmpty(newPassword))
            {
                userToUpdate.Sifre = HashPassword(newPassword);
            }

            try
            {
                _context.Update(userToUpdate);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";
                await RefreshSignInAsync(userToUpdate);

                return RedirectToAction(nameof(ManageProfile));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Concurrency hatası yönetimi
                ModelState.AddModelError(string.Empty, "Aynı anda başka bir işlem profilinizi güncellemeye çalıştı. Lütfen tekrar deneyin.");
                userToUpdate.Sifre = null;
                // Concurrency hatasında da formdaki diğer güncel bilgilerin kalması için
                userToUpdate.Ad = model.Ad;
                userToUpdate.Soyad = model.Soyad;
                userToUpdate.Eposta = model.Eposta;
                userToUpdate.TelefonNumarasi = model.TelefonNumarasi;
                return View("ManageProfile", userToUpdate);
            }
            catch (Exception ex)
            {
                // Genel hata yönetimi
                ModelState.AddModelError(string.Empty, $"Profil güncellenirken bir hata oluştu: {ex.Message}");
                userToUpdate.Sifre = null;
                userToUpdate.Ad = model.Ad;
                userToUpdate.Soyad = model.Soyad;
                userToUpdate.Eposta = model.Eposta;
                userToUpdate.TelefonNumarasi = model.TelefonNumarasi;
                return View("ManageProfile", userToUpdate);
            }
        }

        private async Task RefreshSignInAsync(Kullanicilar user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.KullaniciId.ToString()),
                new Claim(ClaimTypes.Email, user.Eposta),
                new Claim(ClaimTypes.GivenName, user.Ad),
                new Claim(ClaimTypes.Surname, user.Soyad)
            };

            var role = await _context.Roller.FindAsync(user.RolId);
            if (role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.RolAdi));
            }

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        // Şifre hashleme 
        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, 12);
        }
    }
}