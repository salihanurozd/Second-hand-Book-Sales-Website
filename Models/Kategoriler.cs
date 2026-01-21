using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace KitaplikApp.Models
{
    [Table("Kategoriler")]
    public class Kategoriler
    {
        [Key]
        [Column("kategori_id")]
        public int KategoriId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("kategori_adi")]
        public string KategoriAdi { get; set; } = string.Empty;

        public ICollection<Kitaplar>? Kitaplars { get; set; } 

        public Kategoriler()
        {
            Kitaplars = new HashSet<Kitaplar>();
        }
    }
}