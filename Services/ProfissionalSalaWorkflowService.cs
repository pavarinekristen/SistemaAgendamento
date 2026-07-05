using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgendamentoWpfApp.Models;
using AgendamentoWpfApp.Services.Validation;

namespace AgendamentoWpfApp.Services;

internal sealed class ProfissionalSalaWorkflowService
{
    private readonly AgendaWorkspaceService _workspaceService;

    public ProfissionalSalaWorkflowService(AgendaWorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
    }

    public Task<List<ProfissionalSala>> LoadAsync()
    {
        return _workspaceService.LoadProfissionaisAsync();
    }

    public async Task SaveAsync(ProfissionalSala profissionalSala)
    {
        var validation = ProfissionalSalaValidator.Validate(profissionalSala);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.Message);

        await _workspaceService.SaveProfissionalSalaAsync(profissionalSala);
    }

    public Task DeleteAsync(ProfissionalSala profissionalSala)
    {
        return _workspaceService.DeleteProfissionalSalaAsync(profissionalSala);
    }
}
