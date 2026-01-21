using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitaplikApp.Models
{
    [Table("Kitaplar")]
    public class Kitaplar
    {
        [Key]
        [Column("kitap_id")]
        public int KitapId { get; set; }

        [Required]
        [StringLength(200)]
        [Column("baslik")]
        public string Baslik { get; set; } = string.Empty;

        [ForeignKey("Yazar")]
        [Column("yazar_id")]
        public int YazarId { get; set; }
        public Yazarlar? Yazar { get; set; }

        [ForeignKey("Yayinevi")]
        [Column("yayinevi_id")]
        public int YayineviId { get; set; }
        public Yayinevleri? Yayinevi { get; set; }

        [Column("aciklama")]
        public string? Aciklama { get; set; }

        [Column("basim_yili")]
        public int BasimYili { get; set; }

        [Column("sayfa_sayisi")]
        public int SayfaSayisi { get; set; }

        [Column("fiyat", TypeName = "decimal(10, 2)")]
        public decimal Fiyat { get; set; }

        [Required(ErrorMessage = "Stok adedi zorunludur.")]
        [Range(0, 9999, ErrorMessage = "Stok adedi 0 ile 9999 arasında olmalıdır.")]
        [Column("stok")]
        public int Stok { get; set; }

        [ForeignKey("KitapDurumu")]
        [Column("kitap_durumu_id")]
        public int KitapDurumuId { get; set; }
        public KitapDurumlari? KitapDurumu { get; set; }

        [ForeignKey("Kategori")]
        [Column("kategori_id")]
        public int KategoriId { get; set; }
        public Kategoriler? Kategori { get; set; }

        [ForeignKey("SaticiKullanici")]
        [Column("satici_kullanici_id")]
        public int SaticiKullaniciId { get; set; }
        public Kullanicilar? SaticiKullanici { get; set; }

        [Column("yayinlanma_tarihi")]
        public DateTime YayinlanmaTarihi { get; set; }

        [Column("son_guncellenme_tarihi")]
        public DateTime SonGuncellenmeTarihi { get; set; }

        [Required]
        [StringLength(50)]
        [Column("onay_durumu")]
        public string OnayDurumu { get; set; } = "Beklemede";

        public ICollection<FavoriKitaplar>? FavoriKitaplars { get; set; }
        public ICollection<Yorumlar>? Yorumlars { get; set; }
        public ICollection<KitapGorselleri>? KitapGorselleri { get; set; }
        public ICollection<SiparisDetaylari>? SiparisDetaylari { get; set; }
        public ICollection<Mesajlar>? Mesajlar { get; set; }
        public ICollection<SepetDetaylari> SepetDetaylari { get; set; } = new HashSet<SepetDetaylari>();
        public Kitaplar()
        {
            FavoriKitaplars = new HashSet<FavoriKitaplar>();
            Yorumlars = new HashSet<Yorumlar>();
            KitapGorselleri = new HashSet<KitapGorselleri>();
            SiparisDetaylari = new HashSet<SiparisDetaylari>();
            Mesajlar = new HashSet<Mesajlar>();
        }
    }
}