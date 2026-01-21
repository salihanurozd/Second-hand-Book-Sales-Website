using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitaplikApp.Models
{
    [Table("Kullanicilar")]
    public class Kullanicilar
    {
        [Key]
        [Column("kullanici_id")]
        public int KullaniciId { get; set; }

        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [StringLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir.")]
        [Column("ad")]
        public string Ad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        [StringLength(100, ErrorMessage = "Soyad en fazla 100 karakter olabilir.")]
        [Column("soyad")]
        public string Soyad { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta alanı zorunludur.")]
        [StringLength(255, ErrorMessage = "E-posta en fazla 255 karakter olabilir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Column("eposta")]
        public string Eposta { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        [StringLength(255, ErrorMessage = "Şifre en fazla 255 karakter olabilir.")]
        [Column("sifre")]
        public string Sifre { get; set; } = string.Empty;

        [ForeignKey("Rol")]
        [Column("rol_id")]
        public int RolId { get; set; }
        public Roller? Rol { get; set; }

        [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir.")]
        [Column("telefon_numarasi")]
        public string? TelefonNumarasi { get; set; } = string.Empty;

        [Column("kayit_tarihi")]
        public DateTime KayitTarihi { get; set; } = DateTime.Now;

        [Column("son_giris_tarihi")]
        public DateTime? SonGirisTarihi { get; set; }

        [Column("biyografi")]
        public string? Biyografi { get; set; } = string.Empty;

        // Navigasyon Özellikleri (koleksiyonlar):
        public ICollection<Adresler> Adresler { get; set; } = new HashSet<Adresler>();
        public ICollection<Yorumlar> Yorumlars { get; set; } = new HashSet<Yorumlar>();
        public ICollection<FavoriKitaplar> FavoriKitaplars { get; set; } = new HashSet<FavoriKitaplar>();
        public ICollection<Siparisler> Siparisler { get; set; } = new HashSet<Siparisler>();
        public ICollection<Mesajlar> GonderilenMesajlars { get; set; } = new HashSet<Mesajlar>();
        public ICollection<Mesajlar> AlinanMesajlars { get; set; } = new HashSet<Mesajlar>();
        public ICollection<Kitaplar> SatisKitaplars { get; set; } = new HashSet<Kitaplar>();
        public ICollection<Sepetler> Sepetler { get; set; } = new HashSet<Sepetler>();
        
        public Kullanicilar() { }
    }
}