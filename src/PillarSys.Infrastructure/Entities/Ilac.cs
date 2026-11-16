using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PillarSys.Infrastructure.Entities;

[Table("Ilaclar")]
public sealed class Ilac
{
    [Key]
    public int IlacId { get; set; }

    [Required, MaxLength(200)]
    public string IlacAdi { get; set; } = "";

    [MaxLength(50)]
    public string? Barkod { get; set; }

    [MaxLength(100)]
    public string? Kategori { get; set; }

    [MaxLength(30)]
    public string? Birim { get; set; }

    public int StokAdedi { get; set; }

    public int KritikSeviye { get; set; }

    public DateOnly? SonKullanma { get; set; }

    public DateTime GuncellemeTarihi { get; set; } = DateTime.UtcNow;

    public List<StokHareketi> Hareketler { get; set; } = new();
}
