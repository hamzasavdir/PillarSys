using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace PillarSys.AnalyticsRunner;

public sealed class PythonAnalizCalistirici
{
    private readonly string _pythonExe;
    private readonly string _scriptPath;
    private readonly string _workDir;

    public PythonAnalizCalistirici(IConfiguration config)
    {
        _pythonExe = config["Python:Exe"] ?? "python";
        _scriptPath = config["Python:ScriptPath"] ?? "Python\\analiz_uret.py";
        _workDir = AppContext.BaseDirectory;
    }

    public (bool basarili, string hata) Calistir()
    {
        try
        {
            // Script path'i çalıştırma dizinine göre mutlak yap
            var fullScript = Path.IsPathRooted(_scriptPath)
                ? _scriptPath
                : Path.GetFullPath(Path.Combine(_workDir, _scriptPath));

            if (!File.Exists(fullScript))
                return (false, $"Python script bulunamadı: {fullScript}");

            var psi = new ProcessStartInfo
            {
                FileName = _pythonExe,
                Arguments = $"\"{fullScript}\"",
                WorkingDirectory = Path.GetDirectoryName(fullScript) ?? _workDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var p = Process.Start(psi);
            if (p is null) return (false, "Python process başlatılamadı.");

            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();

            if (p.ExitCode != 0)
                return (false, $"Python hata (ExitCode {p.ExitCode}): {stderr}\n{stdout}");

            return (true, "");
        }
        catch (Exception ex)
        {
            return (false, ex.ToString());
        }
    }
}
