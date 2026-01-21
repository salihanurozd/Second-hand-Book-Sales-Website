using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitaplikApp.Models
{
    [Table("Kitap_Durumlari")]
    public class KitapDurumlari
    {
        [Key]
        [Column("durum_id")]
        public int DurumId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("durum_adi")]
        public string DurumAdi { get; set; } = string.Empty;

        public ICollection<Kitaplar>? Kitaplars { get; set; }

        public KitapDurumlari()
        {
            Kitaplars = new HashSet<Kitaplar>();
        }
    }
}