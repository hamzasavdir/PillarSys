using PillarSys.Application.DTO;

namespace PillarSys.Application.Abstractions;

public interface IIlacRepository
{
    Task<List<IlacDto>> TumIlaclariGetirAsync(CancellationToken ct);
    Task<IlacDto?> IlacGetirAsync(int ilacId, CancellationToken ct);
    Task<IlacDto?> IlacGetirAdaGoreAsync(string ilacAdi, CancellationToken ct);
    Task<IlacDto?> IlacGetirBarkodaGoreAsync(string barkod, CancellationToken ct);

    Task<int> IlacEkleAsync(IlacDto yeni, CancellationToken ct);
    Task IlacGuncelleAsync(IlacDto guncel, CancellationToken ct);
}
