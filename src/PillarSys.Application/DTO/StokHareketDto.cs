namespace PillarSys.Application.DTO;

public enum HareketTuru : byte
{
    Giris = 1,
    Cikis = 2,
    Guncelleme = 3
}

public sealed record StokHareketDto(
    long HareketId,
    int IlacId,
    HareketTuru HareketTuru,
    int Miktar,
    string? Aciklama,
    DateTime Tarih
);
