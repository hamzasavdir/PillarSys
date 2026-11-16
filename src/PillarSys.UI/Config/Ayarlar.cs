using System.Text.Json;

namespace PillarSys.UI.Config;

public sealed class Ayarlar
{
    public SqlAyar Sql { get; set; } = new();
    public PythonAyar Python { get; set; } = new();

    public static Ayarlar Yukle(string dosyaYolu)
    {
        var json = File.ReadAllText(dosyaYolu);
        var ayar = JsonSerializer.Deserialize<Ayarlar>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        if (ayar is null) throw new InvalidOperationException("appsettings.json okunamadı.");
        return ayar;
    }
}

public sealed class SqlAyar
{
    public string Server { get; set; } = ".\\SQLEXPRESS";
    public string Database { get; set; } = "PillarSys";
    public bool TrustServerCertificate { get; set; } = true;

    public string MasterConnectionString()
        => $"Server={Server};Database=master;Trusted_Connection=True;TrustServerCertificate={TrustServerCertificate};";

    public string DbConnectionString()
        => $"Server={Server};Database={Database};Trusted_Connection=True;TrustServerCertificate={TrustServerCertificate};";
}

public sealed class PythonAyar
{
    public string PythonExe { get; set; } = "python";
    public string ScriptRelativePath { get; set; } = "Python\\analiz_uret.py";
    public string OutputRelativeFolder { get; set; } = "Python\\output";
}
