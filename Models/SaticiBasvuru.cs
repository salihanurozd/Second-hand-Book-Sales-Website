using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitaplikApp.Models
{
    [Table("SaticiBasvurulari")]
    public class SaticiBasvuru
    {
        [Key]
        [Column("basvuru_id")]
        public int BasvuruId { get; set; }

        [Required]
        [Column("kullanici_id")]
        public int KullaniciId { get; set; }

        [Required]
        [Column("basvuru_tarihi")]
        public DateTime BasvuruTarihi { get; set; } = DateTime.Now;
        
        [ForeignKey("KullaniciId")]
        public Kullanicilar Kullanici { get; set; }
    }
}