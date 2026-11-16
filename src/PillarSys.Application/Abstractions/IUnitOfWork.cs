namespace PillarSys.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> KaydetAsync(CancellationToken ct);
}
