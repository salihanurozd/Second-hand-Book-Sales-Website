using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace KitaplikApp.Models
{
    [Table("Roller")]
    public class Roller
    {
        [Key]
        [Column("rol_id")]
        public int RolId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("rol_adi")]
        public string RolAdi { get; set; } = string.Empty;
        public ICollection<Kullanicilar>? Kullanicilar { get; set; }

        public Roller()
        {
            Kullanicilar = new HashSet<Kullanicilar>();
        }
    }
}