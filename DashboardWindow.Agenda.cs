using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AgendamentoWpfApp.Models;

namespace AgendamentoWpfApp;

public partial class DashboardWindow
{
    private async void AgendaView_DateChanged(object? sender, EventArgs e)
    {
        await LoadConsultasDoDiaAsync();
    }

    private void AgendaView_SelectionChanged(object? sender, EventArgs e)
    {
        if (_loadingConsultaForm)
            return;

        _selectedConsulta = AgendaView.SelectedConsulta;
        LoadConsultaForm(_selectedConsulta);
    }

    private async void AgendaView_SaveRequested(object? sender, EventArgs e)
    {
        await SaveConsultaAsync();
    }

    private async Task SaveConsultaAsync()
    {
        if (!AgendaView.TryBuildFromForm(_selectedConsulta, out var consulta, out var formError))
        {
            SetAgendaStatus(formError, true);
            return;
        }

        try
        {
            await _consultaWorkflow.SaveAsync(consulta);
        }
        catch (InvalidOperationException ex)
        {
            SetAgendaStatus(ex.Message, true);
            return;
        }

        AgendaView.SetSelectedDate(consulta.DataConsulta);
        await LoadConsultasDoDiaAsync();
        AgendaView.SelectConsulta(_consultas.FirstOrDefault(c => c.IdLocal == consulta.IdLocal));
        SetAgendaStatus("Consulta salva. O horario ficou bloqueado para outro agendamento.", false);
    }

    private void AgendaView_NewRequested(object? sender, EventArgs e)
    {
        ClearConsultaForm();
        AgendaView.SelectConsulta(null);
    }

    private async void AgendaView_DeleteRequested(object? sender, EventArgs e)
    {
        if (_selectedConsulta == null)
        {
            SetAgendaStatus("Selecione uma consulta para excluir.", true);
            return;
        }

        var result = MessageBox.Show(
            $"Excluir a consulta de {_selectedConsulta.ClienteNome} em {_selectedConsulta.DataHoraTexto}?",
            "Excluir consulta",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        await _consultaWorkflow.DeleteAsync(_selectedConsulta);
        await LoadConsultasDoDiaAsync();
        ClearConsultaForm();
        SetAgendaStatus("Consulta excluida.", false);
    }

    private void LoadConsultaForm(Consulta? consulta)
    {
        _loadingConsultaForm = true;
        try
        {
            AgendaView.LoadForm(consulta);
        }
        finally
        {
            _loadingConsultaForm = false;
        }
    }

    private void ClearConsultaForm()
    {
        _loadingConsultaForm = true;
        try
        {
            _selectedConsulta = null;
            AgendaView.ClearForm(AgendaView.SelectedDate ?? DateTime.Today);
        }
        finally
        {
            _loadingConsultaForm = false;
        }
    }
}
