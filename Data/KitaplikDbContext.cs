using KitaplikApp.Models;
using Microsoft.EntityFrameworkCore;

namespace KitaplikApp.Data
{
    public class KitaplikDbContext : DbContext
    {
        public KitaplikDbContext(DbContextOptions<KitaplikDbContext> options)
            : base(options)
        {
        }

        public DbSet<Adresler> Adresler { get; set; } = null!;
        public DbSet<Faturalar> Faturalar { get; set; } = null!;
        public DbSet<FavoriKitaplar> FavoriKitaplar { get; set; } = null!; 
        public DbSet<Kategoriler> Kategoriler { get; set; } = null!;
        public DbSet<Kitaplar> Kitaplar { get; set; } = null!;
        public DbSet<KitapDurumlari> KitapDurumlari { get; set; } = null!;
        public DbSet<KitapGorselleri> KitapGorselleri { get; set; } = null!;
        public DbSet<Kullanicilar> Kullanicilar { get; set; } = null!;
        public DbSet<Mesajlar> Mesajlar { get; set; } = null!;
        public DbSet<Roller> Roller { get; set; } = null!;
        public DbSet<Siparisler> Siparisler { get; set; } = null!;
        public DbSet<SiparisDetaylari> SiparisDetaylari { get; set; } = null!;
        public DbSet<Yayinevleri> Yayinevleri { get; set; } = null!;
        public DbSet<Yazarlar> Yazarlar { get; set; } = null!;
        public DbSet<Yorumlar> Yorumlar { get; set; } = null!;
        public DbSet<Sepetler> Sepetler { get; set; } = null!;
        public DbSet<SepetDetaylari> SepetDetaylari { get; set; } = null!;
        public DbSet<SaticiBasvuru> SaticiBasvurulari { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Roller ve Kullanicilar
            modelBuilder.Entity<Kullanicilar>()
                .HasOne(k => k.Rol)
                .WithMany(r => r.Kullanicilar)
                .HasForeignKey(k => k.RolId);

            // Kullanicilar ve Adresler (Kullanıcı'nın birden çok adresi olabilir)
            modelBuilder.Entity<Adresler>()
                .HasOne(a => a.Kullanici)
                .WithMany(k => k.Adresler)
                .HasForeignKey(a => a.KullaniciId)
                .OnDelete(DeleteBehavior.NoAction);

            // Kullanicilar ve Yorumlar
            modelBuilder.Entity<Yorumlar>()
                .HasOne(y => y.Kullanici)
                .WithMany(k => k.Yorumlars)
                .HasForeignKey(y => y.KullaniciId);

            // Kullanicilar ve Favori_Kitaplar -> FavoriKitaplar
            modelBuilder.Entity<FavoriKitaplar>()
                .HasOne(fk => fk.Kullanici)
                .WithMany(k => k.FavoriKitaplars)
                .HasForeignKey(fk => fk.KullaniciId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<FavoriKitaplar>()
                .HasIndex(fk => new { fk.KullaniciId, fk.KitapId })
                .IsUnique();


            // Kullanicilar ve Mesajlar (Gönderen ve Alıcı)
            modelBuilder.Entity<Mesajlar>()
                .HasOne(m => m.GonderenKullanici)
                .WithMany(k => k.GonderilenMesajlars)
                .HasForeignKey(m => m.GonderenKullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Mesajlar>()
                .HasOne(m => m.AliciKullanici)
                .WithMany(k => k.AlinanMesajlars)
                .HasForeignKey(m => m.AliciKullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            // Kullanicilar ve Siparisler (AliciKullanici)
            modelBuilder.Entity<Siparisler>()
                .HasOne(s => s.AliciKullanici)
                .WithMany(k => k.Siparisler)
                .HasForeignKey(s => s.AliciKullaniciId)
                .OnDelete(DeleteBehavior.NoAction);

            // Siparisler ve Adresler (Teslimat Adresi)
            modelBuilder.Entity<Siparisler>()
                .HasOne(s => s.TeslimatAdres)
                .WithMany(a => a.Siparisler)
                .HasForeignKey(s => s.TeslimatAdresId)
                .OnDelete(DeleteBehavior.Restrict);

            // Siparisler ve Faturalar (Birebir ilişki)
            modelBuilder.Entity<Siparisler>()
                .HasOne(s => s.Fatura)
                .WithOne(f => f.Siparis)
                .HasForeignKey<Faturalar>(f => f.SiparisId);

            // Siparisler ve SiparisDetaylari
            modelBuilder.Entity<SiparisDetaylari>()
                .HasOne(sd => sd.Siparis)
                .WithMany(s => s.SiparisDetaylaris)
                .HasForeignKey(sd => sd.SiparisId)
                .OnDelete(DeleteBehavior.Cascade);

            // SiparisDetaylari ve Kitaplar ilişkisi
            modelBuilder.Entity<SiparisDetaylari>()
                .HasOne(sd => sd.Kitap)
                .WithMany(k => k.SiparisDetaylari)
                .HasForeignKey(sd => sd.KitapId)
                .OnDelete(DeleteBehavior.Restrict);

            // Kitaplar ve Kategoriler
            modelBuilder.Entity<Kitaplar>()
                .HasOne(k => k.Kategori)
                .WithMany(c => c.Kitaplars)
                .HasForeignKey(k => k.KategoriId);

            // Kitaplar ve Yazarlar
            modelBuilder.Entity<Kitaplar>()
                .HasOne(k => k.Yazar)
                .WithMany(a => a.Kitaplars)
                .HasForeignKey(k => k.YazarId);

            // Kitaplar ve Yayinevleri
            modelBuilder.Entity<Kitaplar>()
                .HasOne(k => k.Yayinevi)
                .WithMany(p => p.Kitaplars)
                .HasForeignKey(k => k.YayineviId);

            // Kitaplar ve Kitap_Durumlari -> KitapDurumlari
            modelBuilder.Entity<Kitaplar>()
                .HasOne(k => k.KitapDurumu)
                .WithMany(kd => kd.Kitaplars)
                .HasForeignKey(k => k.KitapDurumuId);

            // Kitaplar ve Satici Kullanicilar
            modelBuilder.Entity<Kitaplar>()
                .HasOne(k => k.SaticiKullanici)
                .WithMany(s => s.SatisKitaplars)
                .HasForeignKey(k => k.SaticiKullaniciId)
                .OnDelete(DeleteBehavior.NoAction);

            // Kitaplar ve Favori_Kitaplar -> FavoriKitaplar
            modelBuilder.Entity<FavoriKitaplar>()
                .HasOne(fk => fk.Kitap)
                .WithMany(k => k.FavoriKitaplars)
                .HasForeignKey(fk => fk.KitapId);

            // Kitaplar ve Yorumlar
            modelBuilder.Entity<Yorumlar>()
                .HasOne(y => y.Kitap)
                .WithMany(k => k.Yorumlars)
                .HasForeignKey(y => y.KitapId);

            // Kitaplar ve Kitap_Gorselleri -> KitapGorselleri
            modelBuilder.Entity<KitapGorselleri>()
                .HasOne(kg => kg.Kitap)
                .WithMany(k => k.KitapGorselleri)
                .HasForeignKey(kg => kg.KitapId);

            // Kitaplar ve Mesajlar (Bir kitaba ilişkin mesajlaşmalar olabilir)
            modelBuilder.Entity<Mesajlar>()
                .HasOne(m => m.Kitap)
                .WithMany(k => k.Mesajlar)
                .HasForeignKey(m => m.KitapId);

            // Kullanıcı ile Sepetler arasındaki 1-N ilişkisi
            modelBuilder.Entity<Sepetler>()
                .HasOne(s => s.Kullanici)
                .WithMany(k => k.Sepetler)
                .HasForeignKey(s => s.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);

            // Sepetler ile SepetDetaylari arasındaki 1-N ilişkisi
            modelBuilder.Entity<SepetDetaylari>()
                .HasOne(sd => sd.Sepet)
                .WithMany(s => s.SepetDetaylari)
                .HasForeignKey(sd => sd.SepetId)
                .OnDelete(DeleteBehavior.Cascade);

            // Kitaplar ile SepetDetaylari arasındaki 1-N ilişkisi
            modelBuilder.Entity<SepetDetaylari>()
                .HasOne(sd => sd.Kitap)
                .WithMany(k => k.SepetDetaylari)
                .HasForeignKey(sd => sd.KitapId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}