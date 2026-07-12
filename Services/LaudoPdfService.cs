using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AgendamentoWpfApp.Models;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace AgendamentoWpfApp.Services;

internal sealed class LaudoPdfService
{
    public LaudoPdfResult GerarLaudo(Cliente cliente, Consulta? consulta)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "SparkCore",
            "Laudos");
        Directory.CreateDirectory(folder);

        var tipo = consulta?.TrabalhaArmado == true ? "ComArma" : "SemArma";
        var fileName = $"Laudo_{tipo}_{SanitizeFileName(cliente.Nome)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var path = Path.Combine(folder, fileName);

        var templatePath = TemplatePath();
        if (File.Exists(templatePath))
        {
            ExtrairPaginaTemplate(templatePath, path, consulta?.TrabalhaArmado == true ? 2 : 1);
            return BuildResult(path, tipo);
        }

        File.WriteAllBytes(path, CriarPdf(cliente, consulta));
        return BuildResult(path, tipo);
    }

    private static LaudoPdfResult BuildResult(string path, string tipo)
    {
        return new LaudoPdfResult(
            path,
            new Uri(path).AbsoluteUri,
            Path.GetFileName(path),
            tipo,
            DateTime.Now);
    }

    private static byte[] CriarPdf(Cliente cliente, Consulta? consulta)
    {
        var lines = new List<string>
        {
            "SPARKCORE - LAUDO / RELATORIO DE ATENDIMENTO",
            "",
            "Modelo exemplo para validacao do fluxo de impressao.",
            "",
            $"Cliente: {cliente.Nome}",
            $"CPF: {ValueOrDash(cliente.Cpf)}",
            $"Telefone: {ValueOrDash(cliente.Telefone)}",
            $"E-mail: {ValueOrDash(cliente.Email)}",
            $"Endereco: {ValueOrDash(cliente.Endereco)} {ValueOrDash(cliente.Bairro)} {ValueOrDash(cliente.Cidade)}".Trim(),
            "",
            $"Data da consulta: {consulta?.DataTexto ?? "-"}",
            $"Horario: {ValueOrDash(consulta?.Horario ?? string.Empty)}",
            $"Local: {ValueOrDash(consulta?.Local ?? string.Empty)}",
            $"Profissional/Sala: {ValueOrDash(consulta?.ProfissionalSala ?? string.Empty)}",
            $"Motivo: {ValueOrDash(consulta?.Motivo ?? string.Empty)}",
            $"Tipo de laudo: {ValueOrDash(consulta?.TipoLaudo ?? "Sem arma")}",
            $"Status: {ValueOrDash(consulta?.Status ?? string.Empty)}",
            "",
            "Descricao / resultado:",
            ValueOrDash(consulta?.Observacoes ?? cliente.Observacoes),
            "",
            "Assinatura: ______________________________________________",
            "",
            $"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}"
        };

        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 16 Tf");
        content.AppendLine("50 790 Td");
        content.AppendLine($"({EscapePdf(Normalize(lines[0]))}) Tj");
        content.AppendLine("/F1 10 Tf");

        var yMove = 24;
        foreach (var line in lines.GetRange(1, lines.Count - 1))
        {
            content.AppendLine($"0 -{yMove} Td");
            content.AppendLine($"({EscapePdf(Normalize(line))}) Tj");
            yMove = 17;
        }

        content.AppendLine("ET");
        var stream = Encoding.ASCII.GetBytes(content.ToString());

        var objects = new List<byte[]>
        {
            Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"),
            Encoding.ASCII.GetBytes("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            Encoding.ASCII.GetBytes("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>"),
            Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"),
            Encoding.ASCII.GetBytes($"<< /Length {stream.Length} >>\nstream\n{Encoding.ASCII.GetString(stream)}endstream")
        };

        using var output = new MemoryStream();
        WriteAscii(output, "%PDF-1.4\n");

        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(output.Position);
            WriteAscii(output, $"{i + 1} 0 obj\n");
            output.Write(objects[i]);
            WriteAscii(output, "\nendobj\n");
        }

        var xrefPosition = output.Position;
        WriteAscii(output, $"xref\n0 {objects.Count + 1}\n");
        WriteAscii(output, "0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
            WriteAscii(output, $"{offsets[i]:0000000000} 00000 n \n");

        WriteAscii(output, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefPosition}\n%%EOF");
        return output.ToArray();
    }

    private static void WriteAscii(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string EscapePdf(string value)
    {
        return value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }

    private static string Normalize(string value)
    {
        var normalized = (value ?? string.Empty).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            builder.Append(character <= 127 ? character : '?');
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string ValueOrDash(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    private static string SanitizeFileName(string value)
    {
        var sanitized = string.IsNullOrWhiteSpace(value) ? "Cliente" : value.Trim();
        foreach (var invalid in Path.GetInvalidFileNameChars())
            sanitized = sanitized.Replace(invalid, '_');

        return sanitized.Replace(' ', '_');
    }

    private static string TemplatePath()
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Templates",
            "laudos-oficiais.pdf");
    }

    private static void ExtrairPaginaTemplate(string templatePath, string outputPath, int pagina)
    {
        using var input = PdfReader.Open(templatePath, PdfDocumentOpenMode.Import);
        if (input.PageCount == 0)
            throw new InvalidOperationException("O PDF oficial de laudos nao possui paginas.");

        var pageIndex = Math.Clamp(pagina, 1, input.PageCount) - 1;
        using var output = new PdfDocument();
        output.AddPage(input.Pages[pageIndex]);
        output.Save(outputPath);
    }
}

internal sealed record LaudoPdfResult(
    string Path,
    string Uri,
    string FileName,
    string Tipo,
    DateTime GeradoEm);
