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

    public Task<List<Cliente>> SearchPageAsync(string term, string empresa, int skip, int take)
    {
        return _workspaceService.SearchClientesPageAsync(term, empresa, skip, take);
    }

    public Task<int> CountAsync(string term = "", string empresa = "")
    {
        return _workspaceService.CountClientesAsync(term, empresa);
    }

    public Task<List<string>> ListEmpresasAsync()
    {
        return _workspaceService.ListEmpresasClientesAsync();
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
}
