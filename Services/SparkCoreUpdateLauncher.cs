using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace AgendamentoWpfApp.Services;

internal sealed class SparkCoreUpdateLauncher
{
    private const string UpdaterFileName = "SparkCore.Updater.exe";

    public void TryLaunchOnExit()
    {
        try
        {
            var updaterPath = Path.Combine(AppContext.BaseDirectory, UpdaterFileName);
            if (!File.Exists(updaterPath))
            {
                Log($"Atualizador nao encontrado em {updaterPath}.");
                return;
            }

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

            var startInfo = new ProcessStartInfo
            {
                FileName = updaterPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = AppContext.BaseDirectory
            };

            startInfo.ArgumentList.Add("--parent-pid");
            startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
            startInfo.ArgumentList.Add("--app-dir");
            startInfo.ArgumentList.Add(AppContext.BaseDirectory);
            startInfo.ArgumentList.Add("--data-dir");
            startInfo.ArgumentList.Add(SparkCoreRuntimePaths.DataDirectory);
            startInfo.ArgumentList.Add("--base-url");
            startInfo.ArgumentList.Add(AppSettings.ApiBaseUrl);
            startInfo.ArgumentList.Add("--current-version");
            startInfo.ArgumentList.Add(currentVersion);

            Process.Start(startInfo);
            Log($"Atualizador disparado. VersaoAtual={currentVersion}; BaseUrl={AppSettings.ApiBaseUrl}.");
        }
        catch (Exception ex)
        {
            Log($"Falha ao disparar atualizador: {ex.Message}");
        }
    }

    private static void Log(string message)
    {
        try
        {
            File.AppendAllText(
                Path.Combine(SparkCoreRuntimePaths.LogsDirectory, "updater-launcher.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
            // Falha de log nao pode bloquear o encerramento do sistema.
        }
    }
}
