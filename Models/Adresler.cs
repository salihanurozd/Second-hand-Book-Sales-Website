
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace KitaplikApp.Models
{
    [Table("Adresler")]
    public class Adresler
    {
        [Key]
        [Column("adres_id")]
        public int AdresId { get; set; }

        [ForeignKey("Kullanici")]
        [Column("kullanici_id")]
        public int KullaniciId { get; set; }
        public Kullanicilar? Kullanici { get; set; }

        [Required]
        [StringLength(255)]
        [Column("adres_basligi")]
        public string AdresBasligi { get; set; } = string.Empty; 

        [Required]
        [StringLength(100)]
        [Column("sehir")]
        public string Sehir { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Column("ilce")]
        public string Ilce { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [Column("acik_adres")] 
        public string AcikAdres { get; set; } = string.Empty; 

        [Required]
        [StringLength(50)]
        [Column("posta_kodu")]
        public string PostaKodu { get; set; } = string.Empty;

        public ICollection<Siparisler>? Siparisler { get; set; }

        public Adresler()
        {
            Siparisler = new HashSet<Siparisler>();
        }
    }
}