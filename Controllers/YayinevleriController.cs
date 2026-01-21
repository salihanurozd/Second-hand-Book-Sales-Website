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
    public class YayinevleriController : Controller
    {
        private readonly KitaplikDbContext _context;

        public YayinevleriController(KitaplikDbContext context)
        {
            _context = context;
        }

        // GET: Yayinevleri (Tüm yayınevlerini listeler)
        public async Task<IActionResult> Index()
        {
            return View(await _context.Yayinevleri.ToListAsync());
        }

        // GET: Yayinevleri/Details (Belirli bir yayınevinin detaylarını gösterir)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var yayinevi = await _context.Yayinevleri.FirstOrDefaultAsync(m => m.YayineviId == id);
            
            if (yayinevi == null)
            {
                return NotFound();
            }
            return PartialView("Details", yayinevi);
        }

        // GET: Yayinevleri/Upsert (Yeni yayınevi oluşturma veya düzenleme formu)
        public async Task<IActionResult> Upsert(int? id)
        {
            Yayinevleri yayinevi;
            if (id == null || id == 0)
            {
                yayinevi = new Yayinevleri();
            }
            else
            {
                yayinevi = await _context.Yayinevleri.FindAsync(id);
                if (yayinevi == null)
                {
                    return NotFound();
                }
            }
            return PartialView("Upsert", yayinevi);
        }

        // POST: Yayinevleri/Upsert (Yayınevi oluşturma veya düzenleme işlemi)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert([Bind("YayineviId,YayineviAdi,Adres,Telefon")] Yayinevleri yayinevi)
        {
            if (ModelState.IsValid)
            {
                if (yayinevi.YayineviId == 0)
                {
                    _context.Add(yayinevi);
                }
                else
                {
                    try
                    {
                        _context.Update(yayinevi);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!YayineviExists(yayinevi.YayineviId))
                        {
                            return Json(new { success = false, message = "Güncellenecek yayınevi bulunamadı." });
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                await _context.SaveChangesAsync();
                
                // Başarılı olursa JSON yanıtı döndür
                return Json(new { success = true, message = "Yayınevi başarıyla kaydedildi.", yayineviId = yayinevi.YayineviId });
            }
            
            // Başarısız olursa hata mesajı içeren bir JSON yanıtı döndür
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = "Yayınevi kaydedilirken bir hata oluştu.", errors = errors });
        }

        // POST: Yayinevleri/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var yayinevi = await _context.Yayinevleri.FindAsync(id);
            if (yayinevi == null)
            {
                return Json(new { success = false, message = "Silinecek yayınevi bulunamadı." });
            }
            
            try
            {
                _context.Yayinevleri.Remove(yayinevi);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Yayınevi başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Yayınevi silinirken bir hata oluştu: {ex.Message}" });
            }
        }

        private bool YayineviExists(int id)
        {
            return _context.Yayinevleri.Any(e => e.YayineviId == id);
        }
    }
}