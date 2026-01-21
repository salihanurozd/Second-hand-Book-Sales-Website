using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KitaplikApp.Data;
using KitaplikApp.Models;
using Microsoft.AspNetCore.Authorization;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace KitaplikApp.Controllers
{
    public class KullanicilarController : Controller
    {
        private readonly KitaplikDbContext _context;

        public KullanicilarController(KitaplikDbContext context)
        {
            _context = context;
        }

        // -- KULLANICI YÖNETİMİ  --

        // GET: Kullanicilar/ManageProfile (Kullanıcı kendi profilini yönetir)
        [Authorize]
        public async Task<IActionResult> ManageProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int kullaniciId))
            {
                TempData["ErrorMessage"] = "Kullanıcı kimliği bulunamadı. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Kullanicilar");
            }

            var kullanici = await _context.Kullanicilar
                                        .FirstOrDefaultAsync(m => m.KullaniciId == kullaniciId);

            if (kullanici == null)
            {
                return NotFound();
            }

            var hasPendingApplication = await _context.SaticiBasvurulari.AnyAsync(sa => sa.KullaniciId == kullaniciId);
            ViewBag.HasPendingApplication = hasPendingApplication;

            var viewModel = new KullaniciProfilGuncellemeDto
            {
                Ad = kullanici.Ad,
                Soyad = kullanici.Soyad,
                TelefonNumarasi = kullanici.TelefonNumarasi,
                Biyografi = kullanici.Biyografi
            };

            return View(viewModel);
        }

        // POST: Kullanicilar/ManageProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ManageProfile(KullaniciProfilGuncellemeDto model)
        {
            if (string.IsNullOrEmpty(model.YeniSifre))
            {
                ModelState.Remove("YeniSifre");
                ModelState.Remove("YeniSifreTekrar");
            }
            
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int kullaniciId))
            {
                TempData["ErrorMessage"] = "Kullanıcı kimliği bulunamadı. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Kullanicilar");
            }
            
            var kullaniciToUpdate = await _context.Kullanicilar.FindAsync(kullaniciId);
            if (kullaniciToUpdate == null) return NotFound();

            try
            {
                kullaniciToUpdate.Ad = model.Ad;
                kullaniciToUpdate.Soyad = model.Soyad;
                kullaniciToUpdate.TelefonNumarasi = model.TelefonNumarasi;
                kullaniciToUpdate.Biyografi = model.Biyografi;

                if (!string.IsNullOrEmpty(model.YeniSifre))
                {
                    kullaniciToUpdate.Sifre = HashPassword(model.YeniSifre);
                }

                _context.Update(kullaniciToUpdate);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Profiliniz başarıyla güncellendi.";
                return RedirectToAction(nameof(ManageProfile));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!KullaniciExists(kullaniciToUpdate.KullaniciId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // SATICI BAŞVURU 

        // POST: Kullanici rolündeki kullanıcıdan satıcı olmak için başvuru alır.
        [Authorize(Roles = "Kullanici")] 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyForSeller()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int kullaniciId))
            {
                TempData["ErrorMessage"] = "Kullanıcı kimliği bulunamadı. Lütfen tekrar giriş yapın.";
                return RedirectToAction(nameof(ManageProfile));
            }

            if (await _context.SaticiBasvurulari.AnyAsync(sa => sa.KullaniciId == kullaniciId) || User.IsInRole("Admin") || User.IsInRole("Satici"))
            {
                TempData["ErrorMessage"] = "Zaten bir satıcı başvurunuz var veya satıcı/admin rolündesiniz.";
                return RedirectToAction(nameof(ManageProfile));
            }

            var newApplication = new SaticiBasvuru
            {
                KullaniciId = kullaniciId,
                BasvuruTarihi = DateTime.Now
            };

            _context.SaticiBasvurulari.Add(newApplication);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Satıcı olma başvurunuz alınmıştır. En kısa sürede incelenecektir.";
            return RedirectToAction(nameof(ManageProfile));
        }

        // -- GİRİŞ/KAYIT --

        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Register([Bind("Ad,Soyad,Eposta,Sifre,TelefonNumarasi")] Kullanicilar kullanici)
        {
            RemoveNavPropsFromModelState();
            ModelState.Remove("KullaniciId");
            ModelState.Remove("KayitTarihi");
            ModelState.Remove("SonGirisTarihi");
            ModelState.Remove("Biyografi");

            if (ModelState.IsValid)
            {
                if (await _context.Kullanicilar.AnyAsync(k => k.Eposta == kullanici.Eposta))
                {
                    ModelState.AddModelError("Eposta", "Bu e-posta adresi zaten kullanılıyor.");
                    return View(kullanici);
                }

                kullanici.Sifre = HashPassword(kullanici.Sifre);

                var varsayilanRol = await _context.Roller.FirstOrDefaultAsync(r => r.RolAdi == "Kullanici");
                if (varsayilanRol != null)
                {
                    kullanici.RolId = varsayilanRol.RolId;
                }
                else
                {
                    ModelState.AddModelError("", "Varsayılan kullanıcı rolü bulunamadı. Lütfen 'Kullanici' rolünü ekleyin.");
                    return View(kullanici);
                }

                kullanici.KayitTarihi = DateTime.Now;
                kullanici.SonGirisTarihi = null;
                kullanici.Biyografi = null;

                _context.Add(kullanici);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Kaydınız başarıyla tamamlandı. Giriş yapabilirsiniz.";
                return RedirectToAction("Login");
            }

            return View(kullanici);
        }

        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string eposta, string sifre, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(eposta) || string.IsNullOrEmpty(sifre))
            {
                ModelState.AddModelError("", "E-posta ve şifre gereklidir.");
                return View();
            }

            var kullanici = await _context.Kullanicilar
                                          .Include(k => k.Rol)
                                          .FirstOrDefaultAsync(k => k.Eposta == eposta);

            if (kullanici == null || !BCrypt.Net.BCrypt.Verify(sifre, kullanici.Sifre))
            {
                ModelState.AddModelError("", "Geçersiz e-posta veya şifre.");
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, kullanici.KullaniciId.ToString()),
                new Claim(ClaimTypes.Name, $"{kullanici.Ad} {kullanici.Soyad}"),
                new Claim(ClaimTypes.Email, kullanici.Eposta)
            };

            if (kullanici.Rol != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, kullanici.Rol.RolAdi));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            kullanici.SonGirisTarihi = DateTime.Now;
            _context.Update(kullanici);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "Başarıyla çıkış yaptınız.";
            return RedirectToAction("Login", "Kullanicilar");
        }

        [HttpGet]
        public IActionResult SifremiUnuttum()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SifremiUnuttum(string eposta)
        {
            TempData["SuccessMessage"] = "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi.";
            return RedirectToAction("Login");
        }

        // -- YARDIMCI METOTLAR --

        private bool KullaniciExists(int id)
        {
            return _context.Kullanicilar.Any(e => e.KullaniciId == id);
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, 12);
        }

        private void PopulateDropdowns(Kullanicilar kullanici)
        {
            ViewData["RolId"] = new SelectList(_context.Roller, "RolId", "RolAdi", kullanici.RolId);
            if (kullanici.KullaniciId != 0)
            {
                var kullaniciAdresleriForView = _context.Adresler.AsNoTracking().Where(a => a.KullaniciId == kullanici.KullaniciId).ToList();
                ViewData["AdreslerListesi"] = new SelectList(kullaniciAdresleriForView, "AdresId", "AdresBasligi");
            }
            else
            {
                ViewData["AdreslerListesi"] = new SelectList(Enumerable.Empty<SelectListItem>(), "Value", "Text");
            }
        }
        
        private void RemoveNavPropsFromModelState()
        {
            ModelState.Remove("Rol");
            ModelState.Remove("Adresler");
            ModelState.Remove("Yorumlars");
            ModelState.Remove("FavoriKitaplars");
            ModelState.Remove("Siparisler");
            ModelState.Remove("GonderilenMesajlars");
            ModelState.Remove("AlinanMesajlars");
            ModelState.Remove("SatisKitaplars");
            ModelState.Remove("Sepetler");
        }
    }
}