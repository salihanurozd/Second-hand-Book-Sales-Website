using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering; 
using KitaplikApp.Models; 

namespace KitaplikApp.ViewModels
{
    public class KitapListelemeViewModel
    {
        public IEnumerable<Kitaplar> Kitaplar { get; set; } 
        
        public SelectList Kategoriler { get; set; }
        public SelectList Yazarlar { get; set; }
        
        public string AramaKelimesi { get; set; }
        public int? SecilenKategoriId { get; set; }
        public int? SecilenYazarId { get; set; }
        public decimal? MinFiyat { get; set; }
        public decimal? MaxFiyat { get; set; }
        public string Durum { get; set; }
    }
}