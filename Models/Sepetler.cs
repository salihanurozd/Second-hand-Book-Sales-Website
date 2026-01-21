using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitaplikApp.Models
{
    [Table("Sepetler")]
    public class Sepetler
    {
        [Key]
        [Column("sepet_id")]
        public int SepetId { get; set; }

        [ForeignKey("Kullanici")]
        [Column("kullanici_id")]
        public int KullaniciId { get; set; }
        public Kullanicilar Kullanici { get; set; } = null!; 

        [Column("olusturma_tarihi")]
        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

        [Column("son_guncelleme_tarihi")]
        public DateTime SonGuncellemeTarihi { get; set; } = DateTime.Now;
        public ICollection<SepetDetaylari> SepetDetaylari { get; set; } = new HashSet<SepetDetaylari>();
    }
}