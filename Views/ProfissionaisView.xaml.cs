using System;
using System.Collections;
using System.Windows.Controls;
using AgendamentoWpfApp.Models;
using AgendamentoWpfApp.ViewModels;

namespace AgendamentoWpfApp.Views;

public partial class ProfissionaisView : UserControl
{
    public event EventHandler? SelectionChanged;
    public event EventHandler? SaveRequested;
    public event EventHandler? NewRequested;
    public event EventHandler? DeleteRequested;
    private readonly ProfissionaisViewModel _viewModel;

    public ProfissionaisView()
    {
        InitializeComponent();
        _viewModel = new ProfissionaisViewModel(
            () => SelectionChanged?.Invoke(this, EventArgs.Empty),
            () => SaveRequested?.Invoke(this, EventArgs.Empty),
            () => NewRequested?.Invoke(this, EventArgs.Empty),
            () => DeleteRequested?.Invoke(this, EventArgs.Empty));
        DataContext = _viewModel;
    }

    internal ProfissionalSala? SelectedProfissionalSala => _viewModel.SelectedItem;

    internal ProfissionalSala BuildFromForm(ProfissionalSala? current)
    {
        return _viewModel.BuildFromForm(current);
    }

    public void SetItemsSource(IEnumerable items)
    {
        _viewModel.Items = items;
    }

    public void SetCount(int count)
    {
        _viewModel.SetCount(count);
    }

    internal void SelectItem(ProfissionalSala? item)
    {
        _viewModel.SelectedItem = item;
    }

    internal void LoadForm(ProfissionalSala? item)
    {
        _viewModel.LoadForm(item);
    }

    public void ClearForm()
    {
        _viewModel.ClearForm();
    }

    public void SetStatus(string message, bool isError)
    {
        _viewModel.SetStatus(message, isError);
    }
}
