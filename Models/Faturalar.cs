using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitaplikApp.Models
{
    [Table("Faturalar")]
    public class Faturalar
    {
        [Key]
        [Column("fatura_id")]
        public int FaturaId { get; set; }

        [ForeignKey("Siparis")]
        [Column("siparis_id")]
        public int SiparisId { get; set; }
        public Siparisler? Siparis { get; set; }

        [Required] 
        [Column("fatura_tarihi")]
        public DateTime FaturaTarihi { get; set; }

        
    }
}