using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PillarSys.Infrastructure.Entities;

[Table("StokHareketleri")]
public sealed class StokHareketi
{
    [Key]
    public long HareketId { get; set; }

    [ForeignKey(nameof(Ilac))]
    public int IlacId { get; set; }

    public byte HareketTuru { get; set; } // 1=Giris, 2=Cikis, 3=Guncelleme

    public int Miktar { get; set; }

    [MaxLength(250)]
    public string? Aciklama { get; set; }

    public DateTime Tarih { get; set; } = DateTime.UtcNow;

    public Ilac? Ilac { get; set; }
}
