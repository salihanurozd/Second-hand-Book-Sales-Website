using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitaplikApp.Models
{
    [Table("Sepet_Detaylari")]
    public class SepetDetaylari
    {
        [Key]
        [Column("sepet_detay_id")]
        public int SepetDetayId { get; set; }

        [ForeignKey("Sepet")]
        [Column("sepet_id")]
        public int SepetId { get; set; }
        public Sepetler Sepet { get; set; } = null!;

        [ForeignKey("Kitap")]
        [Column("kitap_id")]
        public int KitapId { get; set; }
        public Kitaplar Kitap { get; set; } = null!;

        [Required]
        [Column("miktar")]
        public int Miktar { get; set; }

        [Required]
        [Column("birim_fiyat", TypeName = "decimal(10, 2)")]
        public decimal BirimFiyat { get; set; } 

        [Column("ekleme_tarihi")]
        public DateTime EklemeTarihi { get; set; } = DateTime.Now;
    }
}