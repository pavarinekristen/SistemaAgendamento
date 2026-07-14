using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AgendamentoWpfApp.Models;

namespace AgendamentoWpfApp;

public partial class DashboardWindow
{
    private void PesquisaClientesView_FilterChanged(object? sender, EventArgs e)
    {
        // Espera o usuario parar de digitar antes de consultar o banco.
        if (_clienteFilterDebounceTimer == null)
            return;

        _clienteFilterDebounceTimer.Stop();
        _clienteFilterDebounceTimer.Start();
    }

    private async void ClienteFilterDebounceTimer_Tick(object? sender, EventArgs e)
    {
        _clienteFilterDebounceTimer?.Stop();

        try
        {
            await RefreshClientesPageAsync(resetPagina: true);
        }
        catch (Exception ex)
        {
            SetFormStatus($"Falha ao pesquisar clientes: {ex.Message}", true);
        }
    }

    private async void PesquisaClientesView_PreviousPageRequested(object? sender, EventArgs e)
    {
        await ChangeClientesPageAsync(-1);
    }

    private async void PesquisaClientesView_NextPageRequested(object? sender, EventArgs e)
    {
        await ChangeClientesPageAsync(1);
    }

    private async Task ChangeClientesPageAsync(int delta)
    {
        try
        {
            _clientesPaginaAtual = Math.Max(0, _clientesPaginaAtual + delta);
            await RefreshClientesPageAsync(resetPagina: false);
        }
        catch (Exception ex)
        {
            SetFormStatus($"Falha ao trocar de pagina: {ex.Message}", true);
        }
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
        // O cliente salvo pode nao estar na pagina atual; selecionar so quando visivel.
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
        ClearClientForm();
        UpdateDetails(null);
        await RefreshClientesPageAsync(resetPagina: false);
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
