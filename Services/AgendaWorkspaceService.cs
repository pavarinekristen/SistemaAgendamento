using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AgendamentoWpfApp.Data.Stores;
using AgendamentoWpfApp.Models;

namespace AgendamentoWpfApp.Services;

internal sealed class AgendaWorkspaceService : IDisposable
{
    private readonly ClienteLocalStore _clienteStore;
    private readonly ConsultaLocalStore _consultaStore;
    private readonly ProfissionalSalaLocalStore _profissionalStore;
    private readonly AgendaSnapshotSyncService _syncService;
    private readonly SyncStatusStore _syncStatusStore;
    private readonly AutomaticAgendaSyncCoordinator _syncCoordinator;

    public AgendaWorkspaceService(string? databasePath = null)
    {
        _clienteStore = new ClienteLocalStore(databasePath);
        _consultaStore = new ConsultaLocalStore(databasePath);
        _profissionalStore = new ProfissionalSalaLocalStore(databasePath);
        _syncStatusStore = new SyncStatusStore(GetSyncStatusFolder(databasePath));
        _syncService = new AgendaSnapshotSyncService(databasePath);
        _syncCoordinator = new AutomaticAgendaSyncCoordinator(_syncService, _syncStatusStore);
        _syncCoordinator.StatusChanged += (message, isError) => SyncStatusChanged?.Invoke(message, isError);
    }

    public event Action<string, bool>? SyncStatusChanged;

    public async Task MigrateAsync()
    {
        await _clienteStore.MigrateAsync();
        await _consultaStore.MigrateAsync();
        await _profissionalStore.MigrateAsync();
    }

    public Task<List<Cliente>> LoadClientesAsync()
    {
        return _clienteStore.LoadAsync();
    }

    public async Task SaveClienteAsync(Cliente cliente)
    {
        await _clienteStore.SaveAsync(cliente);
        _syncCoordinator.MarkPending("cliente-salvo");
    }

    public async Task DeleteClienteAsync(Cliente cliente)
    {
        await _clienteStore.MarkDeletedAsync(cliente);
        _syncCoordinator.MarkPending("cliente-excluido");
    }

    public Task<List<ProfissionalSala>> LoadProfissionaisAsync()
    {
        return _profissionalStore.LoadAsync();
    }

    public async Task SaveProfissionalSalaAsync(ProfissionalSala profissionalSala)
    {
        await _profissionalStore.SaveAsync(profissionalSala);
        _syncCoordinator.MarkPending("profissional-sala-salvo");
    }

    public async Task DeleteProfissionalSalaAsync(ProfissionalSala profissionalSala)
    {
        await _profissionalStore.MarkDeletedAsync(profissionalSala);
        _syncCoordinator.MarkPending("profissional-sala-excluido");
    }

    public Task<List<Consulta>> LoadConsultasDoDiaAsync(DateTime data)
    {
        return _consultaStore.LoadByDateAsync(data);
    }

    public Task<List<Consulta>> LoadConsultasDoClienteAsync(string clienteIdLocal)
    {
        return _consultaStore.LoadByClienteAsync(clienteIdLocal);
    }

    public async Task SaveConsultaAsync(Consulta consulta)
    {
        await _consultaStore.SaveAsync(consulta);
        _syncCoordinator.MarkPending("consulta-salva");
    }

    public async Task DeleteConsultaAsync(Consulta consulta)
    {
        await _consultaStore.MarkDeletedAsync(consulta);
        _syncCoordinator.MarkPending("consulta-excluida");
    }

    public void StartSync()
    {
        _syncCoordinator.Start();
    }

    public SyncStatusSnapshot GetSyncStatus()
    {
        return _syncStatusStore.Load();
    }

    public void Dispose()
    {
        _syncCoordinator.Dispose();
    }

    private static string? GetSyncStatusFolder(string? databasePath)
    {
        return string.IsNullOrWhiteSpace(databasePath)
            ? null
            : Path.GetDirectoryName(databasePath);
    }
}
