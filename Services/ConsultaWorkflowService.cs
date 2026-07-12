using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgendamentoWpfApp.Models;
using AgendamentoWpfApp.Services.Validation;

namespace AgendamentoWpfApp.Services;

internal sealed class ConsultaWorkflowService
{
    private readonly AgendaWorkspaceService _workspaceService;

    public ConsultaWorkflowService(AgendaWorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
    }

    public Task<List<Consulta>> LoadByDateAsync(DateTime data)
    {
        return _workspaceService.LoadConsultasDoDiaAsync(data);
    }

    public Task<List<Consulta>> LoadByClienteAsync(string clienteIdLocal)
    {
        return _workspaceService.LoadConsultasDoClienteAsync(clienteIdLocal);
    }

    public Task<List<Consulta>> LoadReportAsync(
        DateTime? dataInicial,
        DateTime? dataFinal,
        string empresa = "",
        string funcionario = "",
        string motivo = "",
        int take = 1000)
    {
        return _workspaceService.LoadRelatorioConsultasAsync(dataInicial, dataFinal, empresa, funcionario, motivo, take);
    }

    public async Task SaveAsync(Consulta consulta)
    {
        var validation = ConsultaValidator.Validate(consulta);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.Message);

        await _workspaceService.SaveConsultaAsync(consulta);
    }

    public Task DeleteAsync(Consulta consulta)
    {
        return _workspaceService.DeleteConsultaAsync(consulta);
    }
}
