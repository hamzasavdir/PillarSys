namespace PillarSys.Application.Validation;

public sealed class IlacGirdi
{
    public string IlacAdi { get; init; } = "";
    public string? Barkod { get; init; }
    public string? Kategori { get; init; }
    public string? Birim { get; init; }
    public int StokAdedi { get; init; }
    public int KritikSeviye { get; init; }
    public DateOnly? SonKullanma { get; init; }
}

public static class IlacGirdiDogruMu
{
    public static (bool ok, string hata) Kontrol(IlacGirdi girdi)
    {
        if (string.IsNullOrWhiteSpace(girdi.IlacAdi))
            return (false, "İlaç adı boş olamaz.");

        if (girdi.StokAdedi < 0)
            return (false, "Stok adedi negatif olamaz.");

        if (girdi.KritikSeviye < 0)
            return (false, "Kritik seviye negatif olamaz.");

        if (!string.IsNullOrWhiteSpace(girdi.Barkod) && girdi.Barkod!.Length > 50)
            return (false, "Barkod çok uzun.");

        return (true, "");
    }
}
