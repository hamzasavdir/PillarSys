using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PillarSys.AnalyticsRunner;
using PillarSys.Application.Abstractions;
using PillarSys.Application.Services;
using PillarSys.Infrastructure.Bootstrap;
using PillarSys.Infrastructure.Data;
using PillarSys.Infrastructure.Repositories;
using PillarSys.UI.Config;
using Serilog;

namespace PillarSys.UI;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var baseDir = AppContext.BaseDirectory;
            var settingsPath = Path.Combine(baseDir, "appsettings.json");

            // appsettings.json mutlaka output klasörüne kopyalanmış olmalı
            var ayarlar = Ayarlar.Yukle(settingsPath);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(baseDir, "logs", "app-.log"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // DB hazırlığı (DB yoksa oluştur + tablo oluştur)
            var masterCs = ayarlar.Sql.MasterConnectionString();
            var dbName = ayarlar.Sql.Database;

            Func<DbContextOptions<PillarSysDbContext>> optFactory = () =>
            {
                var b = new DbContextOptionsBuilder<PillarSysDbContext>();
                b.UseSqlServer(ayarlar.Sql.DbConnectionString());
                return b.Options;
            };

            // Migration ile uğraşmadan, sağlam çözüm: EnsureCreated
            DatabaseBootstrapper.HazirlaVeEnsureCreatedAsync(masterCs, dbName, optFactory, CancellationToken.None)
                .GetAwaiter().GetResult();

            // DbContext ve servisler
            var db = new PillarSysDbContext(optFactory());

            IIlacRepository ilacRepo = new IlacRepository(db);
            IStokHareketRepository hareketRepo = new StokHareketRepository(db);
            IUnitOfWork uow = new UnitOfWork(db);

            var ilacService = new IlacService(ilacRepo, hareketRepo, uow);

            // Python analiz çalıştırıcı (bizim sınıf: IConfiguration ister)
            // Ayarlar sınıfın JSON'u okuyup tutuyor, bu yüzden burada runner'ı basitçe ayarlardan kullanacağız:
            // Eğer PythonAnalizCalistirici senin projende IConfiguration istiyorsa (biz öyle yaptık),
            // o zaman en sağlam yol: "Minimal config" üretip vermek.

            var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .SetBasePath(baseDir)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var pythonAnaliz = new PythonAnalizCalistirici(config);

            // AnaForm artık 3 parametre alıyor
            System.Windows.Forms.Application.Run(new Forms.AnaForm(ayarlar, ilacService, pythonAnaliz));

        }
        catch (Exception ex)
        {
            try { Log.Fatal(ex, "Uygulama başlatma hatası"); } catch { /* ignore */ }
            MessageBox.Show(ex.ToString(), "PillarSys - Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            try { Log.CloseAndFlush(); } catch { /* ignore */ }
        }
    }
}
