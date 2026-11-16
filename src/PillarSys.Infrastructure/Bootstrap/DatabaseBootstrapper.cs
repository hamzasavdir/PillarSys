using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PillarSys.Infrastructure.Data;

namespace PillarSys.Infrastructure.Bootstrap;

public static class DatabaseBootstrapper
{
    /// <summary>
    /// 1) master'a bağlanır, DB yoksa oluşturur
    /// 2) DbContext ile migrate/ensure create yapar
    /// </summary>
    public static async Task HazirlaVeMigrasyonUygulaAsync(
        string serverConnectionStringToMaster,
        string databaseName,
        Func<DbContextOptions<PillarSysDbContext>> optionsFactory,
        CancellationToken ct)
    {
        await using (var con = new SqlConnection(serverConnectionStringToMaster))
        {
            await con.OpenAsync(ct);

            var cmdText = $@"
IF DB_ID(N'{databaseName}') IS NULL
BEGIN
    CREATE DATABASE [{databaseName}];
END
";
            await using var cmd = new SqlCommand(cmdText, con);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        // DB var -> tablo kur
        await using var db = new PillarSysDbContext(optionsFactory());
        await db.Database.MigrateAsync(ct); // Migration yoksa EnsureCreated yerine geçmez.
        // Not: Migrate için migration üretmek gerekir. Migration istemiyorsan EnsureCreated yap.
    }

    public static async Task HazirlaVeEnsureCreatedAsync(
        string serverConnectionStringToMaster,
        string databaseName,
        Func<DbContextOptions<PillarSysDbContext>> optionsFactory,
        CancellationToken ct)
    {
        await using (var con = new SqlConnection(serverConnectionStringToMaster))
        {
            await con.OpenAsync(ct);

            var cmdText = $@"
IF DB_ID(N'{databaseName}') IS NULL
BEGIN
    CREATE DATABASE [{databaseName}];
END
";
            await using var cmd = new SqlCommand(cmdText, con);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        await using var db = new PillarSysDbContext(optionsFactory());
        await db.Database.EnsureCreatedAsync(ct);
    }
}
