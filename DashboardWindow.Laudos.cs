using System;
using AgendamentoWpfApp.Models;

namespace AgendamentoWpfApp;

public partial class DashboardWindow
{
    private async void LaudosView_ClienteSelectionChanged(object? sender, EventArgs e)
    {
        _laudoConsultas.Clear();
        if (LaudosView.SelectedCliente is not Cliente cliente)
            return;

        foreach (var consulta in await _consultaWorkflow.LoadByClienteAsync(cliente.IdLocal))
            _laudoConsultas.Add(consulta);

        LaudosView.SelectFirstConsultaIfAny();
    }

    private void LaudosView_GerarPdfRequested(object? sender, EventArgs e)
    {
        if (LaudosView.SelectedCliente is not Cliente cliente)
        {
            SetLaudoStatus("Selecione um cliente.", true);
            return;
        }

        var consulta = LaudosView.SelectedConsulta;
        try
        {
            var path = _laudoPdfService.GerarPdfExemplo(cliente, consulta);
            SetLaudoStatus($"PDF exemplo gerado em: {path}", false);
        }
        catch (Exception ex)
        {
            SetLaudoStatus($"Nao foi possivel gerar o PDF: {ex.Message}", true);
        }
    }
}
