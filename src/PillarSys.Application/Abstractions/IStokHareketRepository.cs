using PillarSys.Application.DTO;

namespace PillarSys.Application.Abstractions;

public interface IStokHareketRepository
{
    Task<long> HareketEkleAsync(StokHareketDto hareket, CancellationToken ct);
    Task<List<StokHareketDto>> SonHareketleriGetirAsync(int gunSayisi, CancellationToken ct);
}
