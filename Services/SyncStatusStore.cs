using System;
using System.IO;
using System.Text.Json;

namespace AgendamentoWpfApp.Services;

internal sealed class SyncStatusStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _statusPath;
    private readonly string _logPath;

    public SyncStatusStore(string? folderPath = null)
    {
        var folder = string.IsNullOrWhiteSpace(folderPath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RetaguardaAgendamento")
            : folderPath;

        Directory.CreateDirectory(folder);
        _statusPath = Path.Combine(folder, "sync-status.json");
        _logPath = Path.Combine(folder, "sync-errors.log");
    }

    public SyncStatusSnapshot Load()
    {
        try
        {
            if (!File.Exists(_statusPath))
                return new SyncStatusSnapshot();

            var json = File.ReadAllText(_statusPath);
            return JsonSerializer.Deserialize<SyncStatusSnapshot>(json) ?? new SyncStatusSnapshot();
        }
        catch
        {
            return new SyncStatusSnapshot();
        }
    }

    public SyncStatusSnapshot MarkPending(string origem)
    {
        var snapshot = Load();
        snapshot.Pending = true;
        snapshot.LastOrigin = origem;
        snapshot.LastMessage = "Sincronizacao automatica pendente.";
        snapshot.UpdatedAt = DateTime.Now;
        Save(snapshot);
        return snapshot;
    }

    public SyncStatusSnapshot MarkSuccess(string origem, int totalRegistros)
    {
        var snapshot = Load();
        snapshot.Pending = false;
        snapshot.LastOrigin = origem;
        snapshot.LastSuccessAt = DateTime.Now;
        snapshot.LastMessage = $"Sincronizacao automatica concluida: {totalRegistros} registro(s).";
        snapshot.FailureCount = 0;
        snapshot.NextAttemptAt = null;
        snapshot.UpdatedAt = DateTime.Now;
        Save(snapshot);
        return snapshot;
    }

    public SyncStatusSnapshot MarkFailure(string origem, string message, DateTime? nextAttemptAt)
    {
        var snapshot = Load();
        snapshot.Pending = true;
        snapshot.LastOrigin = origem;
        snapshot.LastErrorAt = DateTime.Now;
        snapshot.LastError = message;
        snapshot.LastMessage = $"Sincronizacao automatica pendente: {message}";
        snapshot.FailureCount++;
        snapshot.NextAttemptAt = nextAttemptAt;
        snapshot.UpdatedAt = DateTime.Now;
        Save(snapshot);
        AppendLog(snapshot);
        return snapshot;
    }

    public void Save(SyncStatusSnapshot snapshot)
    {
        File.WriteAllText(_statusPath, JsonSerializer.Serialize(snapshot, JsonOptions));
    }

    private void AppendLog(SyncStatusSnapshot snapshot)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | origem={snapshot.LastOrigin} | falhas={snapshot.FailureCount} | erro={snapshot.LastError}";
        File.AppendAllText(_logPath, line + Environment.NewLine);
    }
}

internal sealed class SyncStatusSnapshot
{
    public bool Pending { get; set; }
    public string LastOrigin { get; set; } = "";
    public string LastMessage { get; set; } = "";
    public DateTime? LastSuccessAt { get; set; }
    public DateTime? LastErrorAt { get; set; }
    public string LastError { get; set; } = "";
    public int FailureCount { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
