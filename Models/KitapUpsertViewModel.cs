using Microsoft.AspNetCore.Mvc.Rendering;

namespace KitaplikApp.Models
{
    public class KitapUpsertViewModel
    {
        public Kitaplar Kitap { get; set; }
        public SelectList Kategoriler { get; set; }
        public SelectList Yazarlar { get; set; }
        public SelectList Yayinevleri { get; set; }
        public SelectList KitapDurumlari { get; set; }
        public SelectList Saticilar { get; set; }
    }
}