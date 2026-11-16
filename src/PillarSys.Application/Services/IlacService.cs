using PillarSys.Application.Abstractions;
using PillarSys.Application.DTO;
using PillarSys.Application.Validation;

namespace PillarSys.Application.Services;

public sealed class IlacService
{
    private readonly IIlacRepository _ilacRepo;
    private readonly IStokHareketRepository _hareketRepo;
    private readonly IUnitOfWork _uow;

    public IlacService(IIlacRepository ilacRepo, IStokHareketRepository hareketRepo, IUnitOfWork uow)
    {
        _ilacRepo = ilacRepo;
        _hareketRepo = hareketRepo;
        _uow = uow;
    }

    public Task<List<IlacDto>> TumIlaclariGetirAsync(CancellationToken ct)
        => _ilacRepo.TumIlaclariGetirAsync(ct);

    public async Task<(bool ok, string mesaj)> IlacEkleAsync(IlacGirdi girdi, CancellationToken ct)
    {
        var (ok, hata) = IlacGirdiDogruMu.Kontrol(girdi);
        if (!ok) return (false, hata);

        // Aynı ada veya barkoda göre çakışma
        var mevcut = await _ilacRepo.IlacGetirAdaGoreAsync(girdi.IlacAdi.Trim(), ct);
        if (mevcut is not null) return (false, "Bu isimde ilaç zaten var. Güncelle kullanın.");

        if (!string.IsNullOrWhiteSpace(girdi.Barkod))
        {
            var mevcutBarkod = await _ilacRepo.IlacGetirBarkodaGoreAsync(girdi.Barkod.Trim(), ct);
            if (mevcutBarkod is not null) return (false, "Bu barkod zaten kayıtlı.");
        }

        var now = DateTime.UtcNow;

        var yeni = new IlacDto(
            IlacId: 0,
            IlacAdi: girdi.IlacAdi.Trim(),
            Barkod: string.IsNullOrWhiteSpace(girdi.Barkod) ? null : girdi.Barkod.Trim(),
            Kategori: string.IsNullOrWhiteSpace(girdi.Kategori) ? null : girdi.Kategori.Trim(),
            Birim: string.IsNullOrWhiteSpace(girdi.Birim) ? null : girdi.Birim.Trim(),
            StokAdedi: girdi.StokAdedi,
            KritikSeviye: girdi.KritikSeviye,
            SonKullanma: girdi.SonKullanma,
            GuncellemeTarihi: now
        );

        var yeniId = await _ilacRepo.IlacEkleAsync(yeni, ct);

        await _hareketRepo.HareketEkleAsync(new StokHareketDto(
            HareketId: 0,
            IlacId: yeniId,
            HareketTuru: HareketTuru.Giris,
            Miktar: girdi.StokAdedi,
            Aciklama: "İlk kayıt",
            Tarih: now
        ), ct);

        await _uow.KaydetAsync(ct);
        return (true, "İlaç eklendi.");
    }

    public async Task<(bool ok, string mesaj)> StokCikartAsync(string ilacAdi, int miktar, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ilacAdi)) return (false, "İlaç seçin.");
        if (miktar <= 0) return (false, "Miktar 0'dan büyük olmalı.");

        var mevcut = await _ilacRepo.IlacGetirAdaGoreAsync(ilacAdi.Trim(), ct);
        if (mevcut is null) return (false, "İlaç bulunamadı.");

        if (mevcut.StokAdedi - miktar < 0) return (false, "Stok yetersiz.");

        var now = DateTime.UtcNow;
        var guncel = mevcut with { StokAdedi = mevcut.StokAdedi - miktar, GuncellemeTarihi = now };
        await _ilacRepo.IlacGuncelleAsync(guncel, ct);

        await _hareketRepo.HareketEkleAsync(new StokHareketDto(
            HareketId: 0,
            IlacId: mevcut.IlacId,
            HareketTuru: HareketTuru.Cikis,
            Miktar: miktar,
            Aciklama: "Stok çıkışı",
            Tarih: now
        ), ct);

        await _uow.KaydetAsync(ct);
        return (true, "Stok düşüldü.");
    }

    public async Task<(bool ok, string mesaj)> IlacGuncelleAsync(int ilacId, IlacGirdi girdi, CancellationToken ct)
    {
        var (ok, hata) = IlacGirdiDogruMu.Kontrol(girdi);
        if (!ok) return (false, hata);

        var mevcut = await _ilacRepo.IlacGetirAsync(ilacId, ct);
        if (mevcut is null) return (false, "İlaç bulunamadı.");

        var now = DateTime.UtcNow;
        var guncel = mevcut with
        {
            IlacAdi = girdi.IlacAdi.Trim(),
            Barkod = string.IsNullOrWhiteSpace(girdi.Barkod) ? null : girdi.Barkod.Trim(),
            Kategori = string.IsNullOrWhiteSpace(girdi.Kategori) ? null : girdi.Kategori.Trim(),
            Birim = string.IsNullOrWhiteSpace(girdi.Birim) ? null : girdi.Birim.Trim(),
            StokAdedi = girdi.StokAdedi,
            KritikSeviye = girdi.KritikSeviye,
            SonKullanma = girdi.SonKullanma,
            GuncellemeTarihi = now
        };

        await _ilacRepo.IlacGuncelleAsync(guncel, ct);

        await _hareketRepo.HareketEkleAsync(new StokHareketDto(
            HareketId: 0,
            IlacId: mevcut.IlacId,
            HareketTuru: HareketTuru.Guncelleme,
            Miktar: 0,
            Aciklama: "Kayıt güncelleme",
            Tarih: now
        ), ct);

        await _uow.KaydetAsync(ct);
        return (true, "İlaç güncellendi.");
    }
}
