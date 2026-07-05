using System;
using System.Windows.Input;
using System.Windows.Media;
using AgendamentoWpfApp.Services;

namespace AgendamentoWpfApp.ViewModels;

internal sealed class ConfiguracoesViewModel : ViewModelBase
{
    private string _syncStatus = "";
    private string _syncDetails = "";
    private string _backupStatus = "";
    private string _backupDetails = "";
    private Brush _syncStatusBrush = SparkPalette.Success;
    private Brush _backupStatusBrush = SparkPalette.Success;
    private LocalSqliteBackupService? _backupService;

    public ConfiguracoesViewModel(Action logoutRequested, Action restoreBackupRequested)
    {
        LogoutCommand = new RelayCommand(logoutRequested);
        RestoreBackupCommand = new RelayCommand(restoreBackupRequested);
        CreateBackupCommand = new RelayCommand(CreateBackupNow);
        OpenDataFolderCommand = new RelayCommand(OpenDataFolder);
        OpenBackupFolderCommand = new RelayCommand(OpenBackupFolder);
    }

    public string SyncStatus
    {
        get => _syncStatus;
        private set => SetProperty(ref _syncStatus, value);
    }

    public string SyncDetails
    {
        get => _syncDetails;
        private set => SetProperty(ref _syncDetails, value);
    }

    public Brush SyncStatusBrush
    {
        get => _syncStatusBrush;
        private set => SetProperty(ref _syncStatusBrush, value);
    }

    public string BackupStatus
    {
        get => _backupStatus;
        private set => SetProperty(ref _backupStatus, value);
    }

    public string BackupDetails
    {
        get => _backupDetails;
        private set => SetProperty(ref _backupDetails, value);
    }

    public Brush BackupStatusBrush
    {
        get => _backupStatusBrush;
        private set => SetProperty(ref _backupStatusBrush, value);
    }

    public ICommand LogoutCommand { get; }
    public ICommand RestoreBackupCommand { get; }
    public ICommand CreateBackupCommand { get; }
    public ICommand OpenDataFolderCommand { get; }
    public ICommand OpenBackupFolderCommand { get; }

    public void ConfigureBackupService(LocalSqliteBackupService backupService)
    {
        _backupService = backupService;
        RefreshBackupDetails();
    }

    public void SetSyncStatus(string message, bool isError)
    {
        SyncStatus = message;
        SyncStatusBrush = isError ? SparkPalette.Error : SparkPalette.Success;
    }

    public void SetSyncDetails(string details)
    {
        SyncDetails = details;
    }

    public void SetBackupStatus(string message, bool isError)
    {
        BackupStatus = message;
        BackupStatusBrush = isError ? SparkPalette.Error : SparkPalette.Success;
        RefreshBackupDetails();
    }

    private void CreateBackupNow()
    {
        if (_backupService == null)
        {
            SetBackupStatus("Servico de backup nao inicializado.", true);
            return;
        }

        try
        {
            var result = _backupService.CreateBackupNow();
            SetBackupStatus(result.Message, false);
        }
        catch (Exception ex)
        {
            SetBackupStatus($"Falha ao gerar backup: {ex.Message}", true);
        }
    }

    private void OpenDataFolder()
    {
        ExecuteBackupAction(service => service.OpenDataFolder(), "Falha ao abrir pasta de dados");
    }

    private void OpenBackupFolder()
    {
        ExecuteBackupAction(service => service.OpenBackupFolder(), "Falha ao abrir pasta de backups");
    }

    private void ExecuteBackupAction(Action<LocalSqliteBackupService> action, string errorPrefix)
    {
        if (_backupService == null)
        {
            SetBackupStatus("Servico de backup nao inicializado.", true);
            return;
        }

        try
        {
            action(_backupService);
        }
        catch (Exception ex)
        {
            SetBackupStatus($"{errorPrefix}: {ex.Message}", true);
        }
    }

    private void RefreshBackupDetails()
    {
        if (_backupService == null)
        {
            BackupDetails = "Backup local indisponivel.";
            return;
        }

        var snapshot = _backupService.GetSnapshot();
        BackupDetails =
            $"Banco local: {snapshot.DatabasePath}\n" +
            $"Pasta de backups: {snapshot.BackupFolder}\n" +
            $"Backups mantidos: {snapshot.BackupCount}\n" +
            $"Ultimo backup: {FormatDateTime(snapshot.LastBackupAt)}";
    }

    private static string FormatDateTime(DateTime? value)
    {
        return value?.ToString("dd/MM/yyyy HH:mm") ?? "-";
    }
}
