using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using AgendamentoWpfApp.Models;
using AgendamentoWpfApp.Services;
using Microsoft.Win32;

namespace AgendamentoWpfApp;

public partial class DashboardWindow : Window
{
    private readonly AgendaWorkspaceService _workspaceService;
    private readonly ClienteWorkflowService _clienteWorkflow;
    private readonly ConsultaWorkflowService _consultaWorkflow;
    private readonly ProfissionalSalaWorkflowService _profissionalWorkflow;
    private readonly LaudoPdfService _laudoPdfService;
    private readonly LocalSqliteBackupService _backupService;
    private readonly ObservableCollection<Cliente> _clientes = new();
    private readonly ObservableCollection<Consulta> _consultas = new();
    private readonly ObservableCollection<ProfissionalSala> _profissionaisSalas = new();
    private readonly ObservableCollection<Consulta> _laudoConsultas = new();
    private readonly ICollectionView _clientesView;
    private Cliente? _selectedCliente;
    private Consulta? _selectedConsulta;
    private ProfissionalSala? _selectedProfissionalSala;
    private bool _loadingConsultaForm;

    internal DashboardWindow(
        AgendaWorkspaceService workspaceService,
        ClienteWorkflowService clienteWorkflow,
        ConsultaWorkflowService consultaWorkflow,
        ProfissionalSalaWorkflowService profissionalWorkflow,
        LaudoPdfService laudoPdfService,
        LocalSqliteBackupService backupService)
    {
        InitializeComponent();
        SessionSummaryTextBlock.Text = $"{SessionState.UsuarioNome} · {SessionState.EmpresaNome}";
        ApiChipTextBlock.Text = SessionState.BaseUrl;
        SidebarStatusTextBlock.Text = $"Conectado · v{GetAppVersion()}";

        _workspaceService = workspaceService;
        _clienteWorkflow = clienteWorkflow;
        _consultaWorkflow = consultaWorkflow;
        _profissionalWorkflow = profissionalWorkflow;
        _laudoPdfService = laudoPdfService;
        _backupService = backupService;
        _workspaceService.SyncStatusChanged += OnSyncStatusChanged;
        ConfiguracoesView.ConfigureBackupService(_backupService);
        ConfiguracoesView.RestoreBackupRequested += ConfiguracoesView_RestoreBackupRequested;

        _clientesView = CollectionViewSource.GetDefaultView(_clientes);
        _clientesView.Filter = FilterCliente;
        ClientesView.SetItemsSource(_clientesView);
        AgendaView.SetClientesSource(_clientes);
        AgendaView.SetProfissionaisSource(_profissionaisSalas);
        AgendaView.SetConsultasSource(_consultas);
        ProfissionaisView.SetItemsSource(_profissionaisSalas);
        LaudosView.SetClientesSource(_clientes);
        LaudosView.SetConsultasSource(_laudoConsultas);

        AgendaView.SetSelectedDate(DateTime.Today);

        Loaded += DashboardWindow_Loaded;
        Closed += DashboardWindow_Closed;
        ClearClientForm();
        ClearConsultaForm();
        UpdateSummary();
        UpdateSyncStatus("Sincronizacao automatica sera iniciada apos carregar os dados.", false);
    }

    private async void DashboardWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _workspaceService.MigrateAsync();
        CreateDailyBackup();
        await LoadClientesAsync();
        await LoadProfissionaisAsync();
        await LoadConsultasDoDiaAsync();
        _workspaceService.StartSync();
    }

    private void CreateDailyBackup()
    {
        try
        {
            var result = _backupService.CreateDailyBackupIfNeeded();
            ConfiguracoesView.SetBackupStatus(result.Message, false);
        }
        catch (Exception ex)
        {
            ConfiguracoesView.SetBackupStatus($"Falha no backup automatico: {ex.Message}", true);
        }
    }

    private void DashboardWindow_Closed(object? sender, EventArgs e)
    {
        _workspaceService.Dispose();
    }

    private void ConfiguracoesView_RestoreBackupRequested(object? sender, EventArgs e)
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "Selecionar backup do SparkCore",
                InitialDirectory = _backupService.BackupFolder,
                Filter = "Backup SQLite (*.sqlite)|*.sqlite",
                Multiselect = false,
                CheckFileExists = true
            };

            if (dialog.ShowDialog(this) != true)
                return;

            var selectedBackup = Path.GetFullPath(dialog.FileName);
            var backupFolder = Path.GetFullPath(_backupService.BackupFolder);
            if (!selectedBackup.StartsWith(backupFolder, StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(selectedBackup))
            {
                ConfiguracoesView.SetBackupStatus("Selecione um backup valido da pasta de backups do SparkCore.", true);
                return;
            }

            var confirm = MessageBox.Show(
                "Restaurar este backup vai substituir o banco local atual e reiniciar o SparkCore. Um backup de seguranca do estado atual sera criado antes da troca.\n\nContinuar?",
                "Restaurar backup local",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (confirm != MessageBoxResult.Yes)
                return;

            _workspaceService.Dispose();
            var result = _backupService.RestoreBackup(selectedBackup);
            ConfiguracoesView.SetBackupStatus(result.Message, false);

            MessageBox.Show(
                "Backup restaurado. O SparkCore sera reiniciado para recarregar o banco local.",
                "Restore concluido",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            RestartApplication();
        }
        catch (Exception ex)
        {
            ConfiguracoesView.SetBackupStatus($"Falha ao restaurar backup: {ex.Message}", true);
        }
    }

    private static void RestartApplication()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = processPath,
                UseShellExecute = true
            });
        }

        Application.Current.Shutdown();
    }

    private async Task LoadClientesAsync()
    {
        _clientes.Clear();
        foreach (var cliente in await _clienteWorkflow.LoadAsync())
            _clientes.Add(cliente);

        _clientesView.Refresh();
        AgendaView.RefreshClientes();
        UpdateSummary();
        UpdateSyncDetails();
    }

    private async Task LoadProfissionaisAsync()
    {
        _profissionaisSalas.Clear();
        foreach (var item in await _profissionalWorkflow.LoadAsync())
            _profissionaisSalas.Add(item);

        AgendaView.RefreshProfissionais();
        ProfissionaisView.SetCount(_profissionaisSalas.Count);
        UpdateSyncDetails();
    }

    private async Task LoadConsultasDoDiaAsync()
    {
        var data = AgendaView.SelectedDate ?? DateTime.Today;
        _consultas.Clear();
        foreach (var consulta in await _consultaWorkflow.LoadByDateAsync(data))
            _consultas.Add(consulta);

        AgendaView.SetHeader(data, _consultas.Count);
        UpdateSyncDetails();
    }

    private void UpdateSummary()
    {
        var total = _clientes.Count;
        var visiveis = _clientesView.Cast<object>().Count();
        ClientesView.SetCount(total, visiveis, !string.IsNullOrWhiteSpace(ClientesView.SearchTerm));
    }

    private void UpdateSyncStatus(string message, bool isError)
    {
        ConfiguracoesView.SetSyncStatus(message, isError);
    }

    private void OnSyncStatusChanged(string message, bool isError)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateSyncStatus(message, isError);
            UpdateSyncDetails();
        });
    }

    private void UpdateSyncDetails()
    {
        var syncStatus = _workspaceService.GetSyncStatus();
        ConfiguracoesView.SetSyncDetails(
            $"Banco local: {Data.AgendaDbContext.DefaultDatabasePath}\n" +
            "Sistema: SparkCore (SC)\n" +
            $"Versao: {GetAppVersion()}\n" +
            $"API: {SessionState.BaseUrl}\n" +
            $"Clientes carregados: {_clientes.Count}\n" +
            $"Profissionais/salas carregados: {_profissionaisSalas.Count}\n" +
            $"Consultas no dia selecionado: {_consultas.Count}\n" +
            $"Ultimo sucesso sync: {FormatDateTime(syncStatus.LastSuccessAt)}\n" +
            $"Ultimo erro sync: {ValueOrDash(syncStatus.LastError)}\n" +
            $"Proxima tentativa: {FormatDateTime(syncStatus.NextAttemptAt)}\n" +
            "A sincronizacao e automatica: salvar ou excluir dados marca pendencia e o sistema tenta enviar o snapshot para a API.");
    }

    private static string FormatDateTime(DateTime? value)
    {
        return value?.ToString("dd/MM/yyyy HH:mm") ?? "-";
    }

    private static string GetAppVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
    }

    private static string ValueOrDash(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    private void SetFormStatus(string message, bool isError)
    {
        ClientesView.SetStatus(message, isError);
    }

    private void SetAgendaStatus(string message, bool isError)
    {
        AgendaView.SetStatus(message, isError);
    }

    private void SetProfissionalStatus(string message, bool isError)
    {
        ProfissionaisView.SetStatus(message, isError);
    }

    private void SetLaudoStatus(string message, bool isError)
    {
        LaudosView.SetStatus(message, isError);
    }
}
