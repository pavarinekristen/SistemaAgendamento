using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using AgendamentoWpfApp.Models;
using Microsoft.Win32;

namespace AgendamentoWpfApp;

public partial class DashboardWindow
{
    private string _laudoViewerPdfPath = string.Empty;
    private string _laudoViewerFileName = string.Empty;

    private async void LaudosView_FiltrarRequested(object? sender, EventArgs e)
    {
        await LoadLaudosAsync();
    }

    private async void LaudosView_BaixarLaudoRequested(object? sender, EventArgs e)
    {
        if (LaudosView.SelectedConsulta is not Consulta consulta)
        {
            SetLaudoStatus("Selecione um laudo na lista.", true);
            return;
        }

        var cliente = await _clienteWorkflow.FindByIdLocalAsync(consulta.ClienteIdLocal);
        if (cliente == null)
        {
            SetLaudoStatus("Funcionario do laudo nao foi encontrado no cadastro.", true);
            return;
        }

        try
        {
            var pdf = _laudoPdfService.GerarLaudo(cliente, consulta);
            consulta.Status = "Baixado";
            consulta.LaudoTipo = pdf.Tipo;
            consulta.LaudoPdfNomeArquivo = pdf.FileName;
            consulta.LaudoPdfPath = pdf.Path;
            consulta.LaudoPdfUri = pdf.Uri;
            consulta.LaudoGeradoEm = pdf.GeradoEm;
            await _consultaWorkflow.SaveAsync(consulta);
            await AbrirLaudoNoVisualizadorAsync(pdf.Path, pdf.Uri, $"{consulta.ClienteNome} · {consulta.TipoLaudo}", pdf.FileName);
            await LoadConsultasDoDiaAsync();
            await LoadLaudosAsync();
            SetLaudoStatus($"Laudo {consulta.TipoLaudo.ToLowerInvariant()} gerado e marcado para backup na API.", false);
        }
        catch (Exception ex)
        {
            SetLaudoStatus($"Nao foi possivel baixar o laudo: {ex.Message}", true);
        }
    }

    private void LaudosView_ImprimirRequested(object? sender, EventArgs e)
    {
        if (_relatorioLaudos.Count == 0)
        {
            SetLaudoStatus("Nao ha registros para imprimir.", true);
            return;
        }

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
            return;

        var document = BuildLaudosPrintDocument();
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "SparkCore - Relatorio de agendamentos");
        SetLaudoStatus("Relatorio enviado para impressao.", false);
    }

    private async System.Threading.Tasks.Task AbrirLaudoNoVisualizadorAsync(string pdfPath, string pdfUri, string titulo, string fileName)
    {
        _laudoViewerPdfPath = pdfPath;
        _laudoViewerFileName = fileName;
        LaudoViewerTitleTextBlock.Text = titulo;
        LaudoViewerHintTextBlock.Text = string.Empty;

        try
        {
            await LaudoViewerWebView.EnsureCoreWebView2Async();
            LaudoViewerWebView.CoreWebView2.Navigate(new Uri(pdfPath).AbsoluteUri);
            LaudoViewerOverlay.Visibility = Visibility.Visible;
        }
        catch
        {
            // Sem o runtime do WebView2: mantem o comportamento antigo de abrir no navegador.
            AbrirPdfNoNavegador(pdfUri);
        }
    }

    private void LaudoViewerFechar_Click(object sender, RoutedEventArgs e)
    {
        LaudoViewerOverlay.Visibility = Visibility.Collapsed;
        LaudoViewerWebView.CoreWebView2?.Navigate("about:blank"); // solta o arquivo PDF
    }

    private void LaudoViewerBaixar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_laudoViewerPdfPath) || !File.Exists(_laudoViewerPdfPath))
        {
            LaudoViewerHintTextBlock.Text = "Arquivo do laudo nao encontrado.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Salvar laudo",
            FileName = string.IsNullOrWhiteSpace(_laudoViewerFileName) ? "laudo.pdf" : _laudoViewerFileName,
            Filter = "PDF (*.pdf)|*.pdf",
            DefaultExt = ".pdf"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        File.Copy(_laudoViewerPdfPath, dialog.FileName, true);
        LaudoViewerHintTextBlock.Text = "Laudo salvo!";
        SetLaudoStatus($"Laudo salvo em {dialog.FileName}.", false);
    }

    private void LaudoViewerCompartilhar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_laudoViewerPdfPath) || !File.Exists(_laudoViewerPdfPath))
        {
            LaudoViewerHintTextBlock.Text = "Arquivo do laudo nao encontrado.";
            return;
        }

        Clipboard.SetFileDropList(new StringCollection { _laudoViewerPdfPath });
        LaudoViewerHintTextBlock.Text = "Copiado! Cole com Ctrl+V no WhatsApp, e-mail ou pasta.";
    }

    private static void AbrirPdfNoNavegador(string pdfUri)
    {
        if (string.IsNullOrWhiteSpace(pdfUri))
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = pdfUri,
            UseShellExecute = true
        });
    }

    private async void LaudosView_ExcluirRequested(object? sender, EventArgs e)
    {
        if (LaudosView.SelectedConsulta is not Consulta consulta)
        {
            SetLaudoStatus("Selecione um laudo para excluir.", true);
            return;
        }

        var result = MessageBox.Show(
            $"Excluir o agendamento/laudo de {consulta.ClienteNome} em {consulta.DataHoraTexto}?",
            "Excluir laudo",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        await _consultaWorkflow.DeleteAsync(consulta);
        await LoadConsultasDoDiaAsync();
        await LoadLaudosAsync();
        SetLaudoStatus("Laudo excluido.", false);
    }

    private async System.Threading.Tasks.Task LoadLaudosAsync()
    {
        var dataInicial = LaudosView.DataInicial;
        var dataFinal = LaudosView.DataFinal;
        if (dataInicial.HasValue && dataFinal.HasValue && dataInicial.Value > dataFinal.Value)
        {
            SetLaudoStatus("A data inicial nao pode ser maior que a data final.", true);
            return;
        }

        const int limite = 1000;
        var laudos = await _consultaWorkflow.LoadReportAsync(
            dataInicial,
            dataFinal,
            LaudosView.EmpresaTerm,
            LaudosView.FuncionarioTerm,
            LaudosView.SelectedMotivo,
            limite);

        _relatorioLaudos.Reset(laudos);

        var limiteTexto = _relatorioLaudos.Count >= limite ? $" Primeiros {limite} exibidos; refine os filtros." : "";
        SetLaudoStatus($"{_relatorioLaudos.Count} registro(s) encontrado(s).{limiteTexto}", false);
    }

    private FlowDocument BuildLaudosPrintDocument()
    {
        var document = new FlowDocument
        {
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = 11,
            PagePadding = new Thickness(36)
        };

        document.Blocks.Add(new Paragraph(new Run("SparkCore - Relatorio de agendamentos"))
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 12)
        });

        document.Blocks.Add(new Paragraph(new Run(
            $"Periodo: {FormatDate(LaudosView.DataInicial)} ate {FormatDate(LaudosView.DataFinal)} | Empresa: {ValueOrDash(LaudosView.EmpresaTerm)} | Funcionario: {ValueOrDash(LaudosView.FuncionarioTerm)} | Motivo: {ValueOrDash(LaudosView.SelectedMotivo)}"))
        {
            Margin = new Thickness(0, 0, 0, 14)
        });

        var table = new Table();
        document.Blocks.Add(table);

        for (var i = 0; i < 7; i++)
            table.Columns.Add(new TableColumn());

        var group = new TableRowGroup();
        table.RowGroups.Add(group);

        AddRow(group, true, "Data", "Funcionario", "Empresa", "Cargo", "Motivo", "Laudo", "Status");
        foreach (var laudo in _relatorioLaudos)
        {
            AddRow(
                group,
                false,
                laudo.DataHoraTexto,
                laudo.ClienteNome,
                laudo.Empresa,
                laudo.ClienteCargo,
                laudo.Motivo,
                laudo.TipoLaudo,
                laudo.Status);
        }

        return document;
    }

    private static void AddRow(TableRowGroup group, bool header, params string[] values)
    {
        var row = new TableRow();
        group.Rows.Add(row);

        foreach (var value in values)
        {
            row.Cells.Add(new TableCell(new Paragraph(new Run(ValueOrDash(value))))
            {
                FontWeight = header ? FontWeights.Bold : FontWeights.Normal,
                Padding = new Thickness(4),
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 0, 1)
            });
        }
    }

    private static string FormatDate(DateTime? value)
    {
        return value?.ToString("dd/MM/yyyy") ?? "-";
    }
}
