using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitaplikApp.Models
{
    public class KullaniciProfilGuncellemeDto
    {
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [StringLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir.")]
        public string Ad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        [StringLength(100, ErrorMessage = "Soyad en fazla 100 karakter olabilir.")]
        public string Soyad { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir.")]
        public string? TelefonNumarasi { get; set; }

        public string? Biyografi { get; set; }

        // Yeni şifre alanı zorunlu değil
        [DataType(DataType.Password)]
        public string? YeniSifre { get; set; }

        // Yeni şifre tekrar 
        [DataType(DataType.Password)]
        [Compare("YeniSifre", ErrorMessage = "Şifreler eşleşmiyor.")]
        public string? YeniSifreTekrar { get; set; }
    }
}