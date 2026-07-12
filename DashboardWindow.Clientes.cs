using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AgendamentoWpfApp.Models;

namespace AgendamentoWpfApp;

public partial class DashboardWindow
{
    private bool FilterCliente(object item)
    {
        if (item is not Cliente cliente)
            return false;

        var empresa = PesquisaClientesView.SelectedEmpresaFilter;
        if (!string.IsNullOrWhiteSpace(empresa) &&
            !string.Equals(cliente.Empresa, empresa, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return _clienteWorkflow.MatchesSearch(cliente, PesquisaClientesView.SearchTerm);
    }

    private void PesquisaClientesView_FilterChanged(object? sender, EventArgs e)
    {
        _clientesView.Refresh();
        UpdateSummary();
    }

    private void PesquisaClientesView_SelectionChanged(object? sender, EventArgs e)
    {
        UpdateDetails(PesquisaClientesView.SelectedCliente);
    }

    private void PesquisaClientesView_EditRequested(object? sender, EventArgs e)
    {
        if (PesquisaClientesView.SelectedCliente == null)
        {
            PesquisaClientesView.SetDetails(null);
            return;
        }

        _selectedCliente = PesquisaClientesView.SelectedCliente;
        LoadClientForm(_selectedCliente);
        MainTabControl.SelectedIndex = 0;
        SetActiveNav(NavClientesButton);
        SetFormStatus("Cadastro carregado para edicao.", false);
    }

    private async void ClientesView_SaveRequested(object? sender, EventArgs e)
    {
        await SaveClientAsync();
    }

    private async Task SaveClientAsync()
    {
        var cliente = ClientesView.BuildFromForm(_selectedCliente);

        if (_selectedCliente == null)
            _selectedCliente = cliente;

        try
        {
            await _clienteWorkflow.SaveAsync(cliente);
        }
        catch (InvalidOperationException ex)
        {
            SetFormStatus(ex.Message, true);
            ClientesView.FocusNome();
            return;
        }

        await LoadClientesAsync();
        PesquisaClientesView.SelectItem(_clientes.FirstOrDefault(c => c.IdLocal == cliente.IdLocal));
        UpdateDetails(cliente);
        SetFormStatus("Cliente salvo. Ele ja pode ser agendado na aba Agenda.", false);
    }

    private void ClientesView_NewRequested(object? sender, EventArgs e)
    {
        ClearClientForm();
        PesquisaClientesView.SelectItem(null);
        UpdateDetails(null);
    }

    private async void ClientesView_DeleteRequested(object? sender, EventArgs e)
    {
        if (_selectedCliente == null)
        {
            SetFormStatus("Selecione um cliente para excluir.", true);
            return;
        }

        var result = MessageBox.Show(
            $"Excluir o cadastro de {_selectedCliente.Nome}?",
            "Excluir cliente",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        await _clienteWorkflow.DeleteAsync(_selectedCliente);
        _clientes.Remove(_selectedCliente);
        ClearClientForm();
        UpdateDetails(null);
        _clientesView.Refresh();
        UpdateSummary();
        SetFormStatus("Cliente excluido.", false);
    }

    private void LoadClientForm(Cliente? cliente)
    {
        ClientesView.LoadForm(cliente);
    }

    private void ClearClientForm()
    {
        _selectedCliente = null;
        ClientesView.ClearForm();
    }

    private void UpdateDetails(Cliente? cliente)
    {
        PesquisaClientesView.SetDetails(cliente);
    }
}
