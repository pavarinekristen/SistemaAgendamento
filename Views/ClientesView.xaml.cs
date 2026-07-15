using System;
using System.Collections;
using System.Windows.Controls;
using AgendamentoWpfApp.Models;
using AgendamentoWpfApp.ViewModels;

namespace AgendamentoWpfApp.Views;

public partial class ClientesView : UserControl
{
    public event EventHandler? SearchChanged;
    public event EventHandler? SelectionChanged;
    public event EventHandler? SaveRequested;
    public event EventHandler? NewRequested;
    public event EventHandler? DeleteRequested;
    private readonly ClientesViewModel _viewModel;

    public ClientesView()
    {
        InitializeComponent();
        _viewModel = new ClientesViewModel(
            () => SearchChanged?.Invoke(this, EventArgs.Empty),
            () => SelectionChanged?.Invoke(this, EventArgs.Empty),
            () => SaveRequested?.Invoke(this, EventArgs.Empty),
            () => NewRequested?.Invoke(this, EventArgs.Empty),
            () => DeleteRequested?.Invoke(this, EventArgs.Empty));
        DataContext = _viewModel;
    }

    internal Cliente? SelectedCliente => _viewModel.SelectedItem;
    internal string SearchTerm => _viewModel.SearchTerm.Trim();
    internal string SelectedEmpresaFilter => _viewModel.SelectedEmpresaFilter.Trim();

    internal Cliente BuildFromForm(Cliente? current)
    {
        return _viewModel.BuildFromForm(current);
    }

    public void SetItemsSource(IEnumerable items)
    {
        _viewModel.Items = items;
    }

    public void SetEmpresaOptions(IEnumerable empresas)
    {
        _viewModel.SetEmpresaOptions(empresas);
    }

    public void SetCount(int total, int visible, bool hasSearch)
    {
        _viewModel.SetCount(total, visible, hasSearch);
    }

    internal void SelectItem(Cliente? cliente)
    {
        _viewModel.SelectedItem = cliente;
    }

    internal void LoadForm(Cliente? cliente)
    {
        _viewModel.LoadForm(cliente);
    }

    public void ClearForm()
    {
        _viewModel.ClearForm();
    }

    internal void SetDetails(Cliente? cliente)
    {
        _viewModel.SetDetails(cliente);
    }

    public void SetStatus(string message, bool isError)
    {
        _viewModel.SetStatus(message, isError);
    }

    internal void SetSaving(bool saving)
    {
        _viewModel.SetSaving(saving);
    }

    public void FocusNome()
    {
        NomeTextBox.Focus();
    }
}
