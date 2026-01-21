using Microsoft.AspNetCore.Authentication.Cookies; // Cookie Authentication için
using Microsoft.EntityFrameworkCore;
using KitaplikApp.Data;
using KitaplikApp.Models; 
using BCrypt.Net; 

var builder = WebApplication.CreateBuilder(args);

// DbContext servisi
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<KitaplikDbContext>(options =>
    options.UseSqlServer(connectionString));

// Cookie tabanlı kimlik doğrulama servisini ekle
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; 
        options.AccessDeniedPath = "/Account/AccessDenied"; 
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); 
        options.SlidingExpiration = true; 
    });

builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<KitaplikDbContext>();
    dbContext.Database.EnsureCreated(); 
    
    if (!dbContext.Kullanicilar.Any(u => u.Rol.RolAdi == "Admin"))
    {
        var adminRole = dbContext.Roller.FirstOrDefault(r => r.RolAdi == "Admin");
        if (adminRole != null)
        {
            dbContext.Kullanicilar.Add(new Kullanicilar
            {
                Ad = "Yeni",
                Soyad = "Admin",
                Eposta = "ali.tek@gmail.com",
                Sifre = BCrypt.Net.BCrypt.HashPassword("alitek123"),
                RolId = adminRole.RolId,
                KayitTarihi = DateTime.Now,
                SonGirisTarihi = DateTime.Now
            });
            dbContext.SaveChanges();
            Console.WriteLine("Yeni Admin kullanıcısı oluşturuldu");
        }
        else
        {
            Console.WriteLine("Admin rolü bulunamadı, yeni Admin kullanıcısı oluşturulamadı. Lütfen roller tablosunu kontrol edin."); // Konsola bilgi yazdır
        }
    }
}

if (!app.Environment.IsDevelopment()) 
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Kimlik doğrulama
app.UseAuthorization();  // Yetkilendirme

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();