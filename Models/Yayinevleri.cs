using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitaplikApp.Models
{
    [Table("Yayinevleri")]
    public class Yayinevleri
    {
        [Key]
        [Column("yayinevi_id")]
        public int YayineviId { get; set; }

        [Required]
        [StringLength(255)]
        [Column("yayinevi_adi")]
        public string YayineviAdi { get; set; } = string.Empty;

        [StringLength(500)]
        [Column("adres")]
        public string? Adres { get; set; } 

        [StringLength(20)]
        [Column("telefon")]
        public string? Telefon { get; set; } 

        public ICollection<Kitaplar>? Kitaplars { get; set; }

        public Yayinevleri()
        {
            Kitaplars = new HashSet<Kitaplar>();
        }
    }
}