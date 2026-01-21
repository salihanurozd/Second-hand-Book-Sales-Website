using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KitaplikApp.Data;
using KitaplikApp.Models; 

using Microsoft.AspNetCore.Authorization;

namespace KitaplikApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class KitapDurumlariController : Controller
    {
        private readonly KitaplikDbContext _context;

        public KitapDurumlariController(KitaplikDbContext context)
        {
            _context = context;
        }

        // GET: KitapDurumlari 
        public async Task<IActionResult> Index()
        {
            return View(await _context.KitapDurumlari.ToListAsync());
        }

        // GET: KitapDurumlari/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kitapDurumu = await _context.KitapDurumlari
                                            .FirstOrDefaultAsync(m => m.DurumId == id);
            if (kitapDurumu == null)
            {
                return NotFound();
            }

            return View(kitapDurumu);
        }

        // GET: KitapDurumlari/Upsert
        public async Task<IActionResult> Upsert(int? id)
        {
            KitapDurumlari kitapDurumu;

            if (id == null || id == 0) // Yeni kayıt
            {
                kitapDurumu = new KitapDurumlari();
            }
            else // Mevcut kaydı düzenleme
            {
                kitapDurumu = await _context.KitapDurumlari.FindAsync(id);
                if (kitapDurumu == null)
                {
                    return NotFound();
                }
            }
            return View(kitapDurumu);
        }

        // POST: KitapDurumlari/Upsert
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert([Bind("DurumId,DurumAdi")] KitapDurumlari kitapDurumu)
        {
            if (ModelState.IsValid)
            {
                if (kitapDurumu.DurumId == 0) // Yeni kayıt 
                {
                    _context.Add(kitapDurumu);
                    TempData["SuccessMessage"] = "Kitap Durumu başarıyla eklendi.";
                }
                else // Mevcut kaydı güncelle 
                {
                    try
                    {
                        _context.Update(kitapDurumu);
                        TempData["SuccessMessage"] = "Kitap Durumu başarıyla güncellendi.";
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!KitapDurumuExists(kitapDurumu.DurumId))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = "Kitap Durumu kaydedilirken bir hata oluştu. Lütfen bilgileri kontrol edin.";
            return View(kitapDurumu);
        }

        // GET: KitapDurumlari/Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kitapDurumu = await _context.KitapDurumlari
                                            .FirstOrDefaultAsync(m => m.DurumId == id);
            if (kitapDurumu == null)
            {
                return NotFound();
            }

            return View(kitapDurumu);
        }

        // POST: KitapDurumlari/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var kitapDurumu = await _context.KitapDurumlari.FindAsync(id); 
            if (kitapDurumu != null)
            {
                _context.KitapDurumlari.Remove(kitapDurumu);
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Kitap Durumu başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }
        private bool KitapDurumuExists(int id)
        {
            return _context.KitapDurumlari.Any(e => e.DurumId == id);
        }
    }
}