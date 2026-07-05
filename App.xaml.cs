using System;
using System.Windows;
using AgendamentoWpfApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgendamentoWpfApp;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private bool _updateLauncherStarted;

    internal IServiceProvider Services => _serviceProvider
        ?? throw new InvalidOperationException("Services ainda nao foram inicializados.");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        TryLaunchUpdater();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<AgendaWorkspaceService>();
        services.AddSingleton<ClienteWorkflowService>();
        services.AddSingleton<ConsultaWorkflowService>();
        services.AddSingleton<ProfissionalSalaWorkflowService>();
        services.AddSingleton<LaudoPdfService>();
        services.AddSingleton<AuthApiService>();
        services.AddSingleton<SparkCoreUpdateLauncher>();
        services.AddSingleton<LocalSqliteBackupService>();

        services.AddTransient(sp => new MainWindow(
            sp,
            sp.GetRequiredService<AuthApiService>()));
        services.AddTransient(sp => new CreateAccountWindow(
            sp.GetRequiredService<AuthApiService>()));
        services.AddTransient(sp => new DashboardWindow(
            sp.GetRequiredService<AgendaWorkspaceService>(),
            sp.GetRequiredService<ClienteWorkflowService>(),
            sp.GetRequiredService<ConsultaWorkflowService>(),
            sp.GetRequiredService<ProfissionalSalaWorkflowService>(),
            sp.GetRequiredService<LaudoPdfService>(),
            sp.GetRequiredService<LocalSqliteBackupService>()));
    }

    private void TryLaunchUpdater()
    {
        if (_updateLauncherStarted)
            return;

        _updateLauncherStarted = true;

        try
        {
            _serviceProvider?.GetService<SparkCoreUpdateLauncher>()?.TryLaunchOnExit();
        }
        catch
        {
            // Atualizacao nao deve impedir o fechamento do sistema.
        }
    }
}
