using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KitaplikApp.Data;
using KitaplikApp.Models;
using Microsoft.AspNetCore.Authorization;
using System;

namespace KitaplikApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class KategorilerController : Controller
    {
        private readonly KitaplikDbContext _context;

        public KategorilerController(KitaplikDbContext context)
        {
            _context = context;
        }

        // GET: Kategoriler/Index.
        public async Task<IActionResult> Index()
        {
            return View(await _context.Kategoriler.ToListAsync());
        }

        // GET: Kategoriler/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kategori = await _context.Kategoriler
                                         .FirstOrDefaultAsync(m => m.KategoriId == id);
            
            if (kategori == null)
            {
                return NotFound();
            }
            return PartialView("Details", kategori);
        }

        // GET: Kategoriler/Upsert
        public async Task<IActionResult> Upsert(int? id)
        {
            Kategoriler kategori;

            if (id == null || id == 0)
            {
                kategori = new Kategoriler();
            }
            else
            {
                kategori = await _context.Kategoriler.FindAsync(id);
                if (kategori == null)
                {
                    return NotFound();
                }
            }
            return PartialView("Upsert", kategori);
        }

        // POST: Kategoriler/Upsert
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Kategoriler kategori)
        {
            if (ModelState.IsValid)
            {
                if (kategori.KategoriId == 0)
                {
                    _context.Kategoriler.Add(kategori);
                }
                else
                {
                    try
                    {
                        _context.Kategoriler.Update(kategori);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!KategoriExists(kategori.KategoriId))
                        {
                            return Json(new { success = false, message = "Güncellenecek kategori bulunamadı." });
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Kategori başarıyla kaydedildi.", kategoriId = kategori.KategoriId });
            }

            // Doğrulama başarısız olursa
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = "Kategori kaydedilirken bir hata oluştu.", errors = errors });
        }

        // POST: Kategoriler/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var kategori = await _context.Kategoriler.FindAsync(id);
            
            if (kategori == null)
            {
                return Json(new { success = false, message = "Silinecek kategori bulunamadı." });
            }
            
            try
            {
                // Kategoriye bağlı kitapların olup olmadığını kontrol 
                var hasBooks = await _context.Kitaplar.AnyAsync(k => k.KategoriId == kategori.KategoriId);

                if (hasBooks)
                {
                    return Json(new { success = false, message = "Bu kategoriye bağlı kitaplar olduğu için silinemez." });
                }

                _context.Kategoriler.Remove(kategori);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Kategori başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Kategori silinirken bir hata oluştu: {ex.Message}" });
            }
        }

        private bool KategoriExists(int id)
        {
            return _context.Kategoriler.Any(e => e.KategoriId == id);
        }
    }
}