using Microsoft.EntityFrameworkCore;
using PillarSys.Application.Abstractions;
using PillarSys.Application.DTO;
using PillarSys.Infrastructure.Data;
using PillarSys.Infrastructure.Entities;

namespace PillarSys.Infrastructure.Repositories;

public sealed class StokHareketRepository : IStokHareketRepository
{
    private readonly PillarSysDbContext _db;
    public StokHareketRepository(PillarSysDbContext db) => _db = db;

    public async Task<long> HareketEkleAsync(StokHareketDto hareket, CancellationToken ct)
    {
        var e = new StokHareketi
        {
            IlacId = hareket.IlacId,
            HareketTuru = (byte)hareket.HareketTuru,
            Miktar = hareket.Miktar,
            Aciklama = hareket.Aciklama,
            Tarih = hareket.Tarih
        };

        _db.StokHareketleri.Add(e);
        await _db.SaveChangesAsync(ct);
        return e.HareketId;
    }

    public async Task<List<StokHareketDto>> SonHareketleriGetirAsync(int gunSayisi, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Abs(gunSayisi));
        return await _db.StokHareketleri.AsNoTracking()
            .Where(x => x.Tarih >= since)
            .OrderBy(x => x.Tarih)
            .Select(x => new StokHareketDto(
                x.HareketId,
                x.IlacId,
                (HareketTuru)x.HareketTuru,
                x.Miktar,
                x.Aciklama,
                x.Tarih
            ))
            .ToListAsync(ct);
    }
}
