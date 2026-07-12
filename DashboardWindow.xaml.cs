using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
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
    private readonly BulkObservableCollection<Cliente> _clientes = new();
    private readonly BulkObservableCollection<Consulta> _consultas = new();
    private readonly BulkObservableCollection<ProfissionalSala> _profissionaisSalas = new();
    private readonly BulkObservableCollection<Consulta> _relatorioLaudos = new();
    private readonly ICollectionView _clientesView;
    private readonly List<Consulta> _consultasHoje = new();
    private readonly HashSet<string> _lembretesNotificados = new();
    private DispatcherTimer? _notificationTimer;
    private bool _isSidebarCollapsed = true;
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
        ConfigureTopBarProfile();
        SidebarStatusTextBlock.Text = $"SparkCore v{GetAppVersion()}";
        ApplySidebarLayout();

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
        PesquisaClientesView.SetItemsSource(_clientesView);
        AgendaView.SetClienteCandidatesSource(Array.Empty<Cliente>());
        AgendaView.SetProfissionaisSource(_profissionaisSalas);
        AgendaView.SetConsultasSource(_consultas);
        ProfissionaisView.SetItemsSource(_profissionaisSalas);
        LaudosView.SetLaudosSource(_relatorioLaudos);
        LaudosView.SetMotivosSource(new[] { "", "Admissao", "Periodico", "Retorno ao trabalho", "Mudanca de funcao", "Demissional" });

        AgendaView.SetSelectedDate(DateTime.Today);

        _notificationTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _notificationTimer.Tick += NotificationTimer_Tick;
        _notificationTimer.Start();

        Loaded += DashboardWindow_Loaded;
        Closed += DashboardWindow_Closed;
        ClearClientForm();
        ClearConsultaForm();
        UpdateSummary();
        UpdateSyncStatus("Sincronizacao automatica sera iniciada apos carregar os dados.", false);
    }

    private async void DashboardWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdateSyncStatus("Carregando banco local...", false);
            await Task.Yield();

            await _workspaceService.MigrateAsync();
            CreateDailyBackup();
            await LoadClientesAsync();
            await LoadProfissionaisAsync();
            await LoadAgendaClienteCandidatesAsync("");
            await LoadConsultasDoDiaAsync();

            LaudosView.SetStatus("Use os filtros para carregar o historico de laudos.", false);
            _workspaceService.StartSync();
        }
        catch (Exception ex)
        {
            UpdateSyncStatus($"Falha ao abrir dashboard: {ex.Message}", true);
            MessageBox.Show(
                $"Nao foi possivel carregar o dashboard.\n\n{ex.Message}",
                "Erro ao abrir sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
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
        _notificationTimer?.Stop();
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
        _clientes.Reset(await _clienteWorkflow.LoadAsync());

        _clientesView.Refresh();
        var empresas = _clientes
            .Select(c => c.Empresa)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Prepend("")
            .ToList();
        PesquisaClientesView.SetEmpresaOptions(empresas);
        UpdateSummary();
        UpdateSyncDetails();
    }

    private async Task LoadProfissionaisAsync()
    {
        _profissionaisSalas.Reset(await _profissionalWorkflow.LoadAsync());

        AgendaView.RefreshProfissionais();
        ProfissionaisView.SetCount(_profissionaisSalas.Count);
        UpdateSyncDetails();
    }

    private async Task LoadConsultasDoDiaAsync()
    {
        var data = AgendaView.SelectedDate ?? DateTime.Today;
        _consultas.Reset(await _consultaWorkflow.LoadByDateAsync(data));

        AgendaView.SetHeader(data, _consultas.Count);
        await RefreshNotificacoesHojeAsync();
        UpdateSyncDetails();
    }

    private async void NotificationTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            await RefreshNotificacoesHojeAsync();
        }
        catch
        {
            // Lembretes nao podem derrubar o app; a proxima batida do timer tenta de novo.
        }
    }

    private async Task RefreshNotificacoesHojeAsync()
    {
        var consultasHoje = await _consultaWorkflow.LoadByDateAsync(DateTime.Today);

        _consultasHoje.Clear();
        _consultasHoje.AddRange(consultasHoje
            .Where(c => !string.Equals(c.Status, "Cancelada", StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Horario, StringComparer.Ordinal));

        UpdateNotificationBell();
        CheckConsultaReminders();
    }

    private void UpdateNotificationBell()
    {
        var count = _consultasHoje.Count;
        NotificationBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
        NotificationBadgeTextBlock.Text = count > 9 ? "9+" : count.ToString();
        NotificationsEmptyTextBlock.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        NotificationsItemsControl.ItemsSource = _consultasHoje.ToList();
    }

    private void CheckConsultaReminders()
    {
        var agora = DateTime.Now;
        foreach (var consulta in _consultasHoje)
        {
            var statusAtivo =
                string.Equals(consulta.Status, "Agendada", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(consulta.Status, "Aguardando", StringComparison.OrdinalIgnoreCase);

            if (!statusAtivo || !TimeSpan.TryParse(consulta.Horario, out var horario))
                continue;

            var restante = DateTime.Today.Add(horario) - agora;
            if (restante > TimeSpan.Zero &&
                restante <= TimeSpan.FromMinutes(30) &&
                _lembretesNotificados.Add(consulta.IdLocal))
            {
                var minutos = Math.Max(1, (int)Math.Ceiling(restante.TotalMinutes));
                var local = string.IsNullOrWhiteSpace(consulta.Local) ? "" : $" · {consulta.Local}";
                ShowReminderToast(
                    $"Consulta às {consulta.Horario}",
                    $"{consulta.ClienteNome} — faltam {minutos} min{local}.");
            }
        }
    }

    private void ShowReminderToast(string titulo, string mensagem)
    {
        var card = new Border
        {
            Background = (Brush)FindResource("SparkSurfaceBrush"),
            BorderBrush = (Brush)FindResource("SparkLineBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14, 12, 12, 12),
            Margin = new Thickness(0, 0, 0, 10),
            Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#1E140A"),
                BlurRadius = 16,
                ShadowDepth = 4,
                Direction = 270,
                Opacity = 0.22
            }
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var iconChip = new Border
        {
            Width = 34,
            Height = 34,
            CornerRadius = new CornerRadius(17),
            Background = (Brush)FindResource("SparkAccentGradientBrush"),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 11, 0),
            Child = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M18,8 A6,6 0 0 0 6,8 C6,15 3,17 3,17 L21,17 C21,17 18,15 18,8 M13.7,21 A2,2 0 0 1 10.3,21"),
                Stroke = Brushes.White,
                StrokeThickness = 1.8,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Width = 16,
                Height = 16,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        var textos = new StackPanel();
        textos.Children.Add(new TextBlock
        {
            Text = titulo,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        });
        textos.Children.Add(new TextBlock
        {
            Text = mensagem,
            FontSize = 12,
            Foreground = (Brush)FindResource("SparkMutedBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0)
        });
        Grid.SetColumn(textos, 1);

        var fechar = new TextBlock
        {
            Text = "✕",
            FontSize = 12,
            Foreground = (Brush)FindResource("SparkMutedBrush"),
            Cursor = System.Windows.Input.Cursors.Hand,
            Padding = new Thickness(6, 0, 2, 6),
            VerticalAlignment = VerticalAlignment.Top
        };
        fechar.MouseLeftButtonDown += (_, _) => ToastHost.Children.Remove(card);
        Grid.SetColumn(fechar, 2);

        grid.Children.Add(iconChip);
        grid.Children.Add(textos);
        grid.Children.Add(fechar);
        card.Child = grid;

        ToastHost.Children.Insert(0, card);
        card.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
        System.Media.SystemSounds.Exclamation.Play();

        var autoClose = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        autoClose.Tick += (_, _) =>
        {
            autoClose.Stop();
            ToastHost.Children.Remove(card);
        };
        autoClose.Start();
    }

    private void UpdateSummary()
    {
        var total = _clientes.Count;
        var visiveis = _clientesView.Cast<object>().Count();
        ClientesView.SetCount(total, total, false);
        PesquisaClientesView.SetCount(
            total,
            visiveis,
            !string.IsNullOrWhiteSpace(PesquisaClientesView.SearchTerm) ||
            !string.IsNullOrWhiteSpace(PesquisaClientesView.SelectedEmpresaFilter));
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

    private sealed class BulkObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotifications;

        public void Reset(IEnumerable<T> items)
        {
            _suppressNotifications = true;
            try
            {
                Items.Clear();
                foreach (var item in items)
                    Items.Add(item);
            }
            finally
            {
                _suppressNotifications = false;
            }

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotifications)
                base.OnCollectionChanged(e);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!_suppressNotifications)
                base.OnPropertyChanged(e);
        }
    }
}
