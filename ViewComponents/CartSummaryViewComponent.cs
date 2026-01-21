using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using KitaplikApp.Data;
using KitaplikApp.Models;

namespace KitaplikApp.ViewComponents
{
    public class CartSummaryViewComponent : ViewComponent
    {
        private readonly KitaplikDbContext _context;

        public CartSummaryViewComponent(KitaplikDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int cartItemCount = 0;
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kullanıcı giriş yapmış ve Kullanici rolünde ise sepet sayısını getir
            if (User?.Identity != null && User.Identity.IsAuthenticated && HttpContext.User.IsInRole("Kullanici"))
            {
                if (int.TryParse(userId, out int parsedUserId))
                {
                    var sepet = await _context.Sepetler
                                              .Include(s => s.SepetDetaylari)
                                              .FirstOrDefaultAsync(s => s.KullaniciId == parsedUserId);

                    if (sepet != null)
                    {
                        cartItemCount = sepet.SepetDetaylari.Sum(sd => sd.Miktar);
                    }
                }
            }

            // View'a gönderilecek sepet öğe sayısını döndür
            return View("Default", cartItemCount);
        }
    }
}