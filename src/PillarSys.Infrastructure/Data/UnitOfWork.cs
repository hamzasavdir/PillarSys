using PillarSys.Application.Abstractions;

namespace PillarSys.Infrastructure.Data;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly PillarSysDbContext _db;
    public UnitOfWork(PillarSysDbContext db) => _db = db;
    public Task<int> KaydetAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
