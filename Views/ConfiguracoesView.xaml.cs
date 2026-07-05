using System;
using System.Windows.Controls;
using AgendamentoWpfApp.Services;
using AgendamentoWpfApp.ViewModels;

namespace AgendamentoWpfApp.Views;

public partial class ConfiguracoesView : UserControl
{
    public event EventHandler? LogoutRequested;
    public event EventHandler? RestoreBackupRequested;
    private readonly ConfiguracoesViewModel _viewModel;

    public ConfiguracoesView()
    {
        InitializeComponent();
        _viewModel = new ConfiguracoesViewModel(
            () => LogoutRequested?.Invoke(this, EventArgs.Empty),
            () => RestoreBackupRequested?.Invoke(this, EventArgs.Empty));
        DataContext = _viewModel;
    }

    public void SetSyncStatus(string message, bool isError)
    {
        _viewModel.SetSyncStatus(message, isError);
    }

    public void SetSyncDetails(string details)
    {
        _viewModel.SetSyncDetails(details);
    }

    internal void ConfigureBackupService(LocalSqliteBackupService backupService)
    {
        _viewModel.ConfigureBackupService(backupService);
    }

    public void SetBackupStatus(string message, bool isError)
    {
        _viewModel.SetBackupStatus(message, isError);
    }
}
