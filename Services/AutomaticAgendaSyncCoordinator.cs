using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace AgendamentoWpfApp.Services;

internal sealed class AutomaticAgendaSyncCoordinator : IDisposable
{
    private readonly AgendaSnapshotSyncService _syncService;
    private readonly SyncStatusStore _statusStore;
    private readonly DispatcherTimer _timer;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private bool _pending;
    private bool _disposed;

    public AutomaticAgendaSyncCoordinator(AgendaSnapshotSyncService syncService, SyncStatusStore statusStore)
    {
        _syncService = syncService;
        _statusStore = statusStore;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _timer.Tick += async (_, _) => await TrySyncAsync("timer");
    }

    public event Action<string, bool>? StatusChanged;

    public void Start()
    {
        if (_disposed)
            return;

        _pending = true;
        _timer.Start();
        _ = TrySyncAsync("inicio");
    }

    public void MarkPending(string origem)
    {
        if (_disposed)
            return;

        _pending = true;
        _statusStore.MarkPending(origem);
        StatusChanged?.Invoke("Sincronizacao automatica pendente.", false);
        _ = TrySyncAsync(origem);
    }

    private async Task TrySyncAsync(string origem)
    {
        if (_disposed || !_pending)
            return;

        var snapshot = _statusStore.Load();
        if (snapshot.NextAttemptAt.HasValue && snapshot.NextAttemptAt.Value > DateTime.Now)
        {
            StatusChanged?.Invoke($"Sincronizacao automatica aguardando nova tentativa em {snapshot.NextAttemptAt:dd/MM/yyyy HH:mm}.", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(SessionState.BaseUrl) || string.IsNullOrWhiteSpace(SessionState.Token))
        {
            var nextAttempt = DateTime.Now.AddMinutes(1);
            _statusStore.MarkFailure(origem, "Sessao da API nao esta ativa.", nextAttempt);
            StatusChanged?.Invoke("Sincronizacao automatica aguardando sessao ativa.", true);
            return;
        }

        if (!await _syncLock.WaitAsync(0))
            return;

        try
        {
            StatusChanged?.Invoke($"Sincronizando automaticamente ({origem})...", false);
            var result = await _syncService.SincronizarAsync();
            _pending = false;
            var success = _statusStore.MarkSuccess(origem, result.TotalRegistros);
            StatusChanged?.Invoke(success.LastMessage, false);
        }
        catch (Exception ex)
        {
            _pending = true;
            var failureCount = _statusStore.Load().FailureCount + 1;
            var delaySeconds = Math.Min(300, 15 * failureCount);
            var failure = _statusStore.MarkFailure(origem, ex.Message, DateTime.Now.AddSeconds(delaySeconds));
            StatusChanged?.Invoke(failure.LastMessage, true);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _timer.Stop();
        _syncLock.Dispose();
    }
}
