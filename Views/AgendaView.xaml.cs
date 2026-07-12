using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using AgendamentoWpfApp.Models;
using AgendamentoWpfApp.ViewModels;

namespace AgendamentoWpfApp.Views;

public partial class AgendaView : UserControl
{
    public event EventHandler? DateChanged;
    public event EventHandler? SelectionChanged;
    public event EventHandler? ClienteSearchRequested;
    public event EventHandler? SaveRequested;
    public event EventHandler? NewRequested;
    public event EventHandler? DeleteRequested;
    private readonly AgendaViewModel _viewModel;

    public AgendaView()
    {
        InitializeComponent();
        _viewModel = new AgendaViewModel(
            () => DateChanged?.Invoke(this, EventArgs.Empty),
            () => SelectionChanged?.Invoke(this, EventArgs.Empty),
            () => SaveRequested?.Invoke(this, EventArgs.Empty),
            () => NewRequested?.Invoke(this, EventArgs.Empty),
            () => DeleteRequested?.Invoke(this, EventArgs.Empty));
        DataContext = _viewModel;
    }

    internal DateTime? SelectedDate => _viewModel.SelectedDate;
    internal Consulta? SelectedConsulta => _viewModel.SelectedConsulta;
    internal string ClienteSearchTerm => _viewModel.ClienteSearchTerm;

    public void SetClientesSource(IEnumerable items)
    {
        _viewModel.ClienteCandidatesSource = items;
    }

    public void SetClienteCandidatesSource(IEnumerable items)
    {
        _viewModel.ClienteCandidatesSource = items;
    }

    public void SetProfissionaisSource(IEnumerable items)
    {
        _viewModel.ProfissionaisSource = items;
    }

    public void SetConsultasSource(IEnumerable items)
    {
        _viewModel.ConsultasSource = items;
    }

    public void RefreshClientes()
    {
        _viewModel.RefreshClientes();
    }

    public void RefreshProfissionais()
    {
        _viewModel.RefreshProfissionais();
    }

    public void SetSelectedDate(DateTime date)
    {
        _viewModel.SetSelectedDate(date);
    }

    public void SetFormDate(DateTime date)
    {
        _viewModel.FormDate = date.Date;
    }

    public void SetHeader(DateTime date, int count)
    {
        _viewModel.SetHeader(date, count);
    }

    internal bool TryBuildFromForm(Consulta? current, out Consulta consulta, out string errorMessage)
    {
        return _viewModel.TryBuildFromForm(current, out consulta, out errorMessage);
    }

    internal void SelectConsulta(Consulta? consulta)
    {
        _viewModel.SelectedConsulta = consulta;
    }

    internal void SelectCliente(Cliente? cliente)
    {
        _viewModel.SelectedCliente = cliente;
        if (cliente != null)
            _viewModel.ClienteSearchTerm = cliente.Nome;
    }

    internal void LoadForm(Consulta? consulta)
    {
        _viewModel.LoadForm(consulta);
    }

    public void ClearForm(DateTime date)
    {
        _viewModel.ClearForm(date);
    }

    public void SetStatus(string message, bool isError)
    {
        _viewModel.SetStatus(message, isError);
    }

    private void ConsultaHorarioTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _viewModel.NormalizeHorario();
    }

    private void ClienteSearchButton_Click(object sender, RoutedEventArgs e)
    {
        ClienteSearchRequested?.Invoke(this, EventArgs.Empty);
    }
}
