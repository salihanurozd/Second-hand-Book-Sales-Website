using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitaplikApp.Models
{
    [Table("Favori_Kitaplar")]
    public class FavoriKitaplar 
    {
        [Key]
        [Column("favori_id")]
        public int FavoriId { get; set; }

        [ForeignKey("Kullanici")]
        [Column("kullanici_id")]
        public int KullaniciId { get; set; }
        public Kullanicilar? Kullanici { get; set; }

        [ForeignKey("Kitap")]
        [Column("kitap_id")]
        public int KitapId { get; set; }
        public Kitaplar? Kitap { get; set; }
    }
}