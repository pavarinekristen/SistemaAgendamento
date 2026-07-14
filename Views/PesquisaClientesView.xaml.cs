using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using AgendamentoWpfApp.Models;

namespace AgendamentoWpfApp.Views;

public partial class PesquisaClientesView : UserControl
{
    public event EventHandler? FilterChanged;
    public event EventHandler? SelectionChanged;
    public event EventHandler? EditRequested;
    public event EventHandler? PreviousPageRequested;
    public event EventHandler? NextPageRequested;

    public PesquisaClientesView()
    {
        InitializeComponent();
        SetDetails(null);
    }

    internal string SearchTerm => SearchTextBox.Text.Trim();
    internal string SelectedEmpresaFilter => EmpresaComboBox.SelectedItem as string ?? string.Empty;
    internal Cliente? SelectedCliente => ClientesDataGrid.SelectedItem as Cliente;

    public void SetItemsSource(IEnumerable items)
    {
        ClientesDataGrid.ItemsSource = items;
    }

    public void SetEmpresaOptions(IEnumerable<string> empresas)
    {
        EmpresaComboBox.ItemsSource = empresas;
    }

    public void SetCount(int total, int visible, bool hasSearch)
    {
        CountTextBlock.Text = hasSearch
            ? $"{visible} de {total} funcionario(s) encontrado(s)"
            : $"{total} funcionario(s) cadastrado(s)";
    }

    public void SetPageInfo(int pagina, int totalPaginas, int exibindoDe, int exibindoAte, int totalFiltrado)
    {
        PageInfoTextBlock.Text = totalFiltrado == 0
            ? "Nenhum funcionario para exibir."
            : $"Pagina {pagina} de {totalPaginas} — exibindo {exibindoDe} a {exibindoAte} de {totalFiltrado}";
        PaginaAnteriorButton.IsEnabled = pagina > 1;
        ProximaPaginaButton.IsEnabled = pagina < totalPaginas;
    }

    internal void SelectItem(Cliente? cliente)
    {
        ClientesDataGrid.SelectedItem = cliente;
    }

    internal void SetDetails(Cliente? cliente)
    {
        if (cliente == null)
        {
            DetailNameTextBlock.Text = "Nenhum funcionario selecionado";
            DetailContactTextBlock.Text = "";
            DetailAddressTextBlock.Text = "";
            DetailNotesTextBlock.Text = "";
            return;
        }

        DetailNameTextBlock.Text = cliente.Nome;
        DetailContactTextBlock.Text = $"ID: {cliente.IdTexto} | CPF: {ValueOrDash(cliente.CpfFormatado)} | RG: {ValueOrDash(cliente.Rg)} | Tel: {ValueOrDash(cliente.TelefoneFormatado)}";
        DetailAddressTextBlock.Text = $"Empresa: {ValueOrDash(cliente.Empresa)} | Cargo: {ValueOrDash(cliente.Cargo)} | Endereco {ValueOrDash(cliente.TipoEndereco)}: {ValueOrDash(cliente.Endereco)} {ValueOrDash(cliente.Bairro)} {ValueOrDash(cliente.Cidade)}".Trim();
        DetailNotesTextBlock.Text = $"Status: {cliente.Status}. Sexo: {ValueOrDash(cliente.Sexo)}. Escolaridade: {ValueOrDash(cliente.Escolaridade)}. {cliente.Observacoes}";
    }

    private void Filter_Changed(object sender, EventArgs e)
    {
        FilterChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ClientesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void EditarButton_Click(object sender, RoutedEventArgs e)
    {
        EditRequested?.Invoke(this, EventArgs.Empty);
    }

    private void PaginaAnteriorButton_Click(object sender, RoutedEventArgs e)
    {
        PreviousPageRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ProximaPaginaButton_Click(object sender, RoutedEventArgs e)
    {
        NextPageRequested?.Invoke(this, EventArgs.Empty);
    }

    private static string ValueOrDash(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }
}
