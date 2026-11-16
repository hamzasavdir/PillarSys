namespace PillarSys.Application.DTO;

public sealed record IlacDto(
    int IlacId,
    string IlacAdi,
    string? Barkod,
    string? Kategori,
    string? Birim,
    int StokAdedi,
    int KritikSeviye,
    DateOnly? SonKullanma,
    DateTime GuncellemeTarihi
);
