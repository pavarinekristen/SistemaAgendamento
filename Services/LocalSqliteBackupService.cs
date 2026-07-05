using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace AgendamentoWpfApp.Services;

internal sealed class LocalSqliteBackupService
{
    private const int MaxBackups = 10;
    private readonly string _databasePath;
    private readonly string _backupFolder;

    public LocalSqliteBackupService(string? databasePath = null)
    {
        _databasePath = string.IsNullOrWhiteSpace(databasePath)
            ? Data.AgendaDbContext.DefaultDatabasePath
            : databasePath;

        var dataFolder = Path.GetDirectoryName(_databasePath)
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RetaguardaAgendamento");

        _backupFolder = Path.Combine(dataFolder, "Backups");
    }

    public string DatabasePath => _databasePath;
    public string DataFolder => Path.GetDirectoryName(_databasePath) ?? string.Empty;
    public string BackupFolder => _backupFolder;

    public LocalBackupSnapshot GetSnapshot()
    {
        var backups = ListBackups();
        return new LocalBackupSnapshot(
            _databasePath,
            _backupFolder,
            backups.Count,
            backups.FirstOrDefault()?.CreationTime);
    }

    public LocalBackupResult CreateDailyBackupIfNeeded()
    {
        Directory.CreateDirectory(_backupFolder);

        if (!File.Exists(_databasePath))
            return LocalBackupResult.Skipped("Banco local ainda nao existe.");

        var todayPrefix = $"agenda-{DateTime.Now:yyyyMMdd}-";
        if (ListBackups().Any(f => f.Name.StartsWith(todayPrefix, StringComparison.OrdinalIgnoreCase)))
            return LocalBackupResult.Skipped("Backup diario ja existe.");

        return CreateBackup("automatico");
    }

    public LocalBackupResult CreateBackupNow()
    {
        Directory.CreateDirectory(_backupFolder);

        if (!File.Exists(_databasePath))
            return LocalBackupResult.Skipped("Banco local ainda nao existe.");

        return CreateBackup("manual");
    }

    public LocalBackupResult RestoreBackup(string backupPath)
    {
        Directory.CreateDirectory(_backupFolder);

        if (string.IsNullOrWhiteSpace(backupPath))
            throw new InvalidOperationException("Selecione um arquivo de backup.");

        var sourcePath = Path.GetFullPath(backupPath);
        var backupFolder = Path.GetFullPath(_backupFolder);
        if (!sourcePath.StartsWith(backupFolder, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("O arquivo selecionado precisa estar na pasta de backups do SparkCore.");

        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Backup selecionado nao foi encontrado.", sourcePath);

        if (!Path.GetFileName(sourcePath).StartsWith("agenda-", StringComparison.OrdinalIgnoreCase) ||
            !sourcePath.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Arquivo de backup invalido.");
        }

        Directory.CreateDirectory(DataFolder);
        SqliteConnection.ClearAllPools();

        string? safetyBackupPath = null;
        if (File.Exists(_databasePath))
        {
            safetyBackupPath = Path.Combine(_backupFolder, $"agenda-before-restore-{DateTime.Now:yyyyMMdd-HHmmss}.sqlite");
            File.Copy(_databasePath, safetyBackupPath, overwrite: false);
        }

        File.Copy(sourcePath, _databasePath, overwrite: true);
        ApplyRetention();

        var message = safetyBackupPath == null
            ? $"Backup restaurado: {Path.GetFileName(sourcePath)}"
            : $"Backup restaurado: {Path.GetFileName(sourcePath)}. Copia anterior: {Path.GetFileName(safetyBackupPath)}";

        return LocalBackupResult.Created(message, sourcePath);
    }

    public void OpenDataFolder()
    {
        Directory.CreateDirectory(DataFolder);
        OpenFolder(DataFolder);
    }

    public void OpenBackupFolder()
    {
        Directory.CreateDirectory(_backupFolder);
        OpenFolder(_backupFolder);
    }

    private LocalBackupResult CreateBackup(string origin)
    {
        var backupPath = Path.Combine(_backupFolder, $"agenda-{DateTime.Now:yyyyMMdd-HHmmss}-{origin}.sqlite");

        using var source = new SqliteConnection($"Data Source={_databasePath}");
        using var destination = new SqliteConnection($"Data Source={backupPath}");

        source.Open();
        destination.Open();
        source.BackupDatabase(destination);

        ApplyRetention();
        return LocalBackupResult.Created(backupPath);
    }

    private IReadOnlyList<FileInfo> ListBackups()
    {
        if (!Directory.Exists(_backupFolder))
            return Array.Empty<FileInfo>();

        return new DirectoryInfo(_backupFolder)
            .EnumerateFiles("agenda-*.sqlite")
            .OrderByDescending(f => f.CreationTimeUtc)
            .ToList();
    }

    private void ApplyRetention()
    {
        foreach (var file in ListBackups().Skip(MaxBackups))
        {
            try
            {
                file.Delete();
            }
            catch
            {
                // Falha ao apagar backup antigo nao deve impedir o backup atual.
            }
        }
    }

    private static void OpenFolder(string folder)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true
        });
    }
}

internal sealed record LocalBackupSnapshot(
    string DatabasePath,
    string BackupFolder,
    int BackupCount,
    DateTime? LastBackupAt);

internal sealed record LocalBackupResult(bool WasCreated, string Message, string? BackupPath)
{
    public static LocalBackupResult Created(string backupPath)
    {
        return new LocalBackupResult(true, $"Backup criado: {Path.GetFileName(backupPath)}", backupPath);
    }

    public static LocalBackupResult Created(string message, string backupPath)
    {
        return new LocalBackupResult(true, message, backupPath);
    }

    public static LocalBackupResult Skipped(string message)
    {
        return new LocalBackupResult(false, message, null);
    }
}
