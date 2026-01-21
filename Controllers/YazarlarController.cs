using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KitaplikApp.Data;
using KitaplikApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace KitaplikApp.Controllers
{
    [Authorize(Roles = "Admin,Satıcı")]
    public class YazarlarController : Controller
    {
        private readonly KitaplikDbContext _context;

        public YazarlarController(KitaplikDbContext context)
        {
            _context = context;
        }

        // GET: Yazarlar (yazarları listele)
        public async Task<IActionResult> Index()
        {
            return View(await _context.Yazarlar.ToListAsync());
        }

        // GET: Yazarlar/Details (Belirli bir yazarın detaylarını modal'da göster)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var yazar = await _context.Yazarlar.FirstOrDefaultAsync(m => m.YazarId == id);
            if (yazar == null)
            {
                return NotFound();
            }
            return PartialView("Details", yazar);
        }

        // GET: Yazarlar/Upsert 
        public async Task<IActionResult> Upsert(int? id)
        {
            Yazarlar yazar;
            if (id == null || id == 0)
            {
                yazar = new Yazarlar();
            }
            else
            {
                yazar = await _context.Yazarlar.FindAsync(id);
                if (yazar == null)
                {
                    return NotFound();
                }
            }
            return PartialView("Upsert", yazar);
        }

        // POST: Yazarlar/Upsert
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert([Bind("YazarId,YazarAdi,Biyografi")] Yazarlar yazar)
        {
            if (ModelState.IsValid)
            {
                if (yazar.YazarId == 0)
                {
                    _context.Add(yazar);
                }
                else
                {
                    try
                    {
                        _context.Update(yazar);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!YazarExists(yazar.YazarId))
                        {
                            return Json(new { success = false, message = "Güncellenecek yazar bulunamadı." });
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, message = "Yazar başarıyla kaydedildi.", yazarId = yazar.YazarId });
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = "Yazar kaydedilirken bir hata oluştu.", errors = errors });
        }

        // POST: Yazarlar/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var yazar = await _context.Yazarlar.FindAsync(id);
            if (yazar == null)
            {
                return Json(new { success = false, message = "Silinecek yazar bulunamadı." });
            }
            
            try
            {
                // Yazara bağlı kitapların olup olmadığını kontrol 
                var hasBooks = await _context.Kitaplar.AnyAsync(k => k.YazarId == yazar.YazarId);

                if (hasBooks)
                {
                    return Json(new { success = false, message = "Bu yazara bağlı kitaplar olduğu için silinemez." });
                }

                _context.Yazarlar.Remove(yazar);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Yazar başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Yazar silinirken bir hata oluştu: {ex.Message}" });
            }
        }

        private bool YazarExists(int id)
        {
            return _context.Yazarlar.Any(e => e.YazarId == id);
        }
    }
}