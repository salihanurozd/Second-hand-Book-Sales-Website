using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitaplikApp.Models
{
    [Table("Yazarlar")]
    public class Yazarlar
    {
        [Key]
        [Column("yazar_id")]
        public int YazarId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("yazar_adi")]
        public string YazarAdi { get; set; } = string.Empty;

        [Column("biyografi")]
        public string? Biyografi { get; set; } 

        public ICollection<Kitaplar>? Kitaplars { get; set; }

        public Yazarlar()
        {
            Kitaplars = new HashSet<Kitaplar>();
        }
    }
}