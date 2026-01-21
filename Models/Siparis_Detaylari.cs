using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitaplikApp.Models
{
    [Table("Siparis_Detaylari")]
    public class SiparisDetaylari
    {
        [Key]
        [Column("siparis_detay_id")]
        public int SiparisDetayId { get; set; }

        [ForeignKey("Siparis")]
        [Column("siparis_id")]
        public int SiparisId { get; set; }
        public Siparisler? Siparis { get; set; }

        [ForeignKey("Kitap")]
        [Column("kitap_id")]
        public int KitapId { get; set; }
        public Kitaplar? Kitap { get; set; }

        [Required] 
        [Column("adet")]
        public int Adet { get; set; }

        [Required] 
        [Column("birim_fiyat", TypeName = "decimal(10, 2)")]
        public decimal BirimFiyat { get; set; }
    }
}