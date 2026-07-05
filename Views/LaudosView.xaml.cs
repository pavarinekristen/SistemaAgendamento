using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgendamentoWpfApp.Models;

namespace AgendamentoWpfApp.Views;

public partial class LaudosView : UserControl
{
    public event EventHandler? ClienteSelectionChanged;
    public event EventHandler? GerarPdfRequested;

    public LaudosView()
    {
        InitializeComponent();
    }

    internal Cliente? SelectedCliente => LaudoClienteComboBox.SelectedItem as Cliente;

    internal Consulta? SelectedConsulta => LaudoConsultaComboBox.SelectedItem as Consulta;

    public void SetClientesSource(IEnumerable clientes)
    {
        LaudoClienteComboBox.ItemsSource = clientes;
    }

    public void SetConsultasSource(IEnumerable consultas)
    {
        LaudoConsultaComboBox.ItemsSource = consultas;
    }

    public void SelectFirstConsultaIfAny()
    {
        LaudoConsultaComboBox.SelectedIndex = LaudoConsultaComboBox.Items.Count > 0 ? 0 : -1;
    }

    public void SetStatus(string message, bool isError)
    {
        LaudoStatusTextBlock.Text = message;
        LaudoStatusTextBlock.Foreground = isError
            ? SparkPalette.Error
            : SparkPalette.Success;
    }

    private void LaudoClienteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ClienteSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void GerarPdfExemploButton_Click(object sender, RoutedEventArgs e)
    {
        GerarPdfRequested?.Invoke(this, EventArgs.Empty);
    }
}
