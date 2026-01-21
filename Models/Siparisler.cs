using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitaplikApp.Models
{
    [Table("Siparisler")]
    public class Siparisler
    {
        [Key]
        [Column("siparis_id")]
        public int SiparisId { get; set; }

        [ForeignKey("AliciKullanici")]
        [Column("alici_kullanici_id")]
        public int AliciKullaniciId { get; set; }
        public Kullanicilar? AliciKullanici { get; set; }

        [Required]
        [Column("siparis_tarihi")]
        public DateTime SiparisTarihi { get; set; }

        [Required]
        [Column("toplam_tutar", TypeName = "decimal(10, 2)")]
        public decimal ToplamTutar { get; set; }

        [StringLength(50)]
        [Column("siparis_durumu")]
        public string SiparisDurumu { get; set; } = string.Empty;

        [ForeignKey("TeslimatAdres")]
        [Column("teslimat_adres_id")]
        public int TeslimatAdresId { get; set; }
        public Adresler? TeslimatAdres { get; set; }

        
        public Faturalar? Fatura { get; set; } 

        public ICollection<SiparisDetaylari>? SiparisDetaylaris { get; set; }

        public Siparisler()
        {
            SiparisDetaylaris = new HashSet<SiparisDetaylari>();
        }
    }
}