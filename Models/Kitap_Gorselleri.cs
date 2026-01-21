using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitaplikApp.Models
{
    [Table("Kitap_Gorselleri")]
    public class KitapGorselleri
    {
        [Key]
        [Column("resim_id")]
        public int ResimId { get; set; }

        [ForeignKey("Kitap")]
        [Column("kitap_id")]
        public int KitapId { get; set; }
        public Kitaplar? Kitap { get; set; }

        [Required]
        [StringLength(500)]
        [Column("resim_url")]
        public string ResimUrl { get; set; } = string.Empty;

        [Column("gosterim_sirasi")] 
        public int GosterimSirasi { get; set; } 
    }
}