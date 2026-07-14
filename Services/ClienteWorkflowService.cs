using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgendamentoWpfApp.Models;
using AgendamentoWpfApp.Services.Validation;

namespace AgendamentoWpfApp.Services;

internal sealed class ClienteWorkflowService
{
    private readonly AgendaWorkspaceService _workspaceService;

    public ClienteWorkflowService(AgendaWorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
    }

    public Task<List<Cliente>> LoadAsync()
    {
        return _workspaceService.LoadClientesAsync();
    }

    public Task<List<Cliente>> SearchAsync(string term, int take = 50)
    {
        return _workspaceService.SearchClientesAsync(term, take);
    }

    public Task<Cliente?> FindByIdLocalAsync(string idLocal)
    {
        return _workspaceService.FindClienteByIdLocalAsync(idLocal);
    }

    public async Task SaveAsync(Cliente cliente)
    {
        var validation = ClienteValidator.Validate(cliente);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.Message);

        await _workspaceService.SaveClienteAsync(cliente);
    }

    public Task DeleteAsync(Cliente cliente)
    {
        return _workspaceService.DeleteClienteAsync(cliente);
    }

    public bool MatchesSearch(Cliente cliente, string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return true;

        term = term.Trim();

        // Termo numerico tambem encontra pelo ID (ex.: "12" ou "0012").
        if (int.TryParse(term, out var idBusca) && idBusca > 0 && cliente.Id == idBusca)
            return true;

        return Contains(cliente.Nome, term)
            || Contains(cliente.Empresa, term)
            || Contains(cliente.Cargo, term)
            || Contains(cliente.Cpf, term)
            || Contains(cliente.Rg, term)
            || Contains(cliente.Email, term)
            || Contains(cliente.Telefone, term)
            || Contains(cliente.Status, term);
    }

    private static bool Contains(string value, string term)
    {
        return (value ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
