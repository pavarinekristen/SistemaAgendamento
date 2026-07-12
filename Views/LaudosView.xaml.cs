using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgendamentoWpfApp.Models;

namespace AgendamentoWpfApp.Views;

public partial class LaudosView : UserControl
{
    public event EventHandler? FiltrarRequested;
    public event EventHandler? BaixarLaudoRequested;
    public event EventHandler? ImprimirRequested;
    public event EventHandler? ExcluirRequested;

    public LaudosView()
    {
        InitializeComponent();
        DataInicialDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
        DataFinalDatePicker.SelectedDate = DateTime.Today;
    }

    internal DateTime? DataInicial => DataInicialDatePicker.SelectedDate?.Date;
    internal DateTime? DataFinal => DataFinalDatePicker.SelectedDate?.Date;
    internal string EmpresaTerm => EmpresaTextBox.Text.Trim();
    internal string FuncionarioTerm => FuncionarioTextBox.Text.Trim();
    internal string SelectedMotivo => MotivoComboBox.SelectedItem as string ?? string.Empty;
    internal Consulta? SelectedConsulta => LaudosDataGrid.SelectedItem as Consulta;

    public void SetClientesSource(IEnumerable clientes)
    {
        // Mantido por compatibilidade com o dashboard. A tela de laudos pesquisa por texto
        // para nao materializar 48 mil funcionarios em um ComboBox.
    }

    public void SetEmpresasSource(IEnumerable<string> empresas)
    {
        // Mantido por compatibilidade. Empresa tambem e filtro textual para evitar dropdown pesado.
    }

    public void SetMotivosSource(IEnumerable<string> motivos)
    {
        MotivoComboBox.ItemsSource = motivos;
    }

    public void SetLaudosSource(IEnumerable consultas)
    {
        LaudosDataGrid.ItemsSource = consultas;
    }

    public void SetStatus(string message, bool isError)
    {
        LaudoStatusTextBlock.Text = message;
        LaudoStatusTextBlock.Foreground = isError
            ? SparkPalette.Error
            : SparkPalette.Success;
    }

    private void FiltrarButton_Click(object sender, RoutedEventArgs e)
    {
        FiltrarRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BaixarLaudoButton_Click(object sender, RoutedEventArgs e)
    {
        BaixarLaudoRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ImprimirButton_Click(object sender, RoutedEventArgs e)
    {
        ImprimirRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ExcluirButton_Click(object sender, RoutedEventArgs e)
    {
        ExcluirRequested?.Invoke(this, EventArgs.Empty);
    }
}
