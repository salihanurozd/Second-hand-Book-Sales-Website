using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitaplikApp.Models
{
    [Table("Mesajlar")]
    public class Mesajlar
    {
        [Key]
        [Column("mesaj_id")]
        public int MesajId { get; set; }

        [ForeignKey("GonderenKullanici")]
        [Column("gonderen_kullanici_id")]
        public int GonderenKullaniciId { get; set; }
        public Kullanicilar? GonderenKullanici { get; set; }

        [ForeignKey("AliciKullanici")]
        [Column("alici_kullanici_id")]
        public int AliciKullaniciId { get; set; }
        public Kullanicilar? AliciKullanici { get; set; }

        [ForeignKey("Kitap")]
        [Column("kitap_id")]
        public int? KitapId { get; set; } 
        public Kitaplar? Kitap { get; set; }

        [Required]
        [StringLength(255)]
        [Column("konu")] 
        public string Konu { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        [Column("icerik")]
        public string Icerik { get; set; } = string.Empty;

        [Required]
        [Column("gonderim_tarihi")]
        public DateTime GonderimTarihi { get; set; }

        [Column("okundu_mu")]
        public bool OkunduMu { get; set; }
    }
}