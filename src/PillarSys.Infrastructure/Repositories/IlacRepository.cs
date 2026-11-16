using Microsoft.EntityFrameworkCore;
using PillarSys.Application.Abstractions;
using PillarSys.Application.DTO;
using PillarSys.Infrastructure.Data;
using PillarSys.Infrastructure.Entities;

namespace PillarSys.Infrastructure.Repositories;

public sealed class IlacRepository : IIlacRepository
{
    private readonly PillarSysDbContext _db;
    public IlacRepository(PillarSysDbContext db) => _db = db;

    public async Task<List<IlacDto>> TumIlaclariGetirAsync(CancellationToken ct)
        => await _db.Ilaclar
            .AsNoTracking()
            .OrderBy(x => x.IlacAdi)
            // Not: Projeyi farklı makinelerde daha sağlam derlemek için Map method-group yerine
            // Expression uyumlu inline projection kullanıyoruz.
            .Select(x => new IlacDto(
                x.IlacId,
                x.IlacAdi,
                x.Barkod,
                x.Kategori,
                x.Birim,
                x.StokAdedi,
                x.KritikSeviye,
                x.SonKullanma,
                x.GuncellemeTarihi
            ))
            .ToListAsync(ct);

    public async Task<IlacDto?> IlacGetirAsync(int ilacId, CancellationToken ct)
    {
        var e = await _db.Ilaclar.AsNoTracking().FirstOrDefaultAsync(x => x.IlacId == ilacId, ct);
        return e is null ? null : Map(e);
    }

    public async Task<IlacDto?> IlacGetirAdaGoreAsync(string ilacAdi, CancellationToken ct)
    {
        var e = await _db.Ilaclar.AsNoTracking().FirstOrDefaultAsync(x => x.IlacAdi == ilacAdi, ct);
        return e is null ? null : Map(e);
    }

    public async Task<IlacDto?> IlacGetirBarkodaGoreAsync(string barkod, CancellationToken ct)
    {
        var e = await _db.Ilaclar.AsNoTracking().FirstOrDefaultAsync(x => x.Barkod == barkod, ct);
        return e is null ? null : Map(e);
    }

    public async Task<int> IlacEkleAsync(IlacDto yeni, CancellationToken ct)
    {
        var e = new Ilac
        {
            IlacAdi = yeni.IlacAdi,
            Barkod = yeni.Barkod,
            Kategori = yeni.Kategori,
            Birim = yeni.Birim,
            StokAdedi = yeni.StokAdedi,
            KritikSeviye = yeni.KritikSeviye,
            SonKullanma = yeni.SonKullanma,
            GuncellemeTarihi = yeni.GuncellemeTarihi
        };

        _db.Ilaclar.Add(e);
        await _db.SaveChangesAsync(ct);
        return e.IlacId;
    }

    public async Task IlacGuncelleAsync(IlacDto guncel, CancellationToken ct)
    {
        var e = await _db.Ilaclar.FirstOrDefaultAsync(x => x.IlacId == guncel.IlacId, ct);
        if (e is null) throw new InvalidOperationException("İlaç bulunamadı.");

        e.IlacAdi = guncel.IlacAdi;
        e.Barkod = guncel.Barkod;
        e.Kategori = guncel.Kategori;
        e.Birim = guncel.Birim;
        e.StokAdedi = guncel.StokAdedi;
        e.KritikSeviye = guncel.KritikSeviye;
        e.SonKullanma = guncel.SonKullanma;
        e.GuncellemeTarihi = guncel.GuncellemeTarihi;

        await _db.SaveChangesAsync(ct);
    }

    private static IlacDto Map(Ilac x) => new(
        x.IlacId, x.IlacAdi, x.Barkod, x.Kategori, x.Birim, x.StokAdedi, x.KritikSeviye, x.SonKullanma, x.GuncellemeTarihi
    );
}
