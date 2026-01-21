using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitaplikApp.Models
{
    [Table("Yorumlar")]
    public class Yorumlar
    {
        [Key]
        [Column("yorum_id")]
        public int YorumId { get; set; }

        [ForeignKey("Kitap")]
        [Column("kitap_id")]
        public int KitapId { get; set; }
        public Kitaplar? Kitap { get; set; }

        [ForeignKey("Kullanici")]
        [Column("kullanici_id")]
        public int KullaniciId { get; set; }
        public Kullanicilar? Kullanici { get; set; }

        [Required]
        [Column("puan")] 
        public int Puan { get; set; }

        [Required]
        [StringLength(1000)]
        [Column("yorum_metni")]
        public string YorumMetni { get; set; } = string.Empty;

        [Required]
        [Column("yorum_tarihi")]
        public DateTime YorumTarihi { get; set; }
    }
}