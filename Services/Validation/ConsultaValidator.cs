using System;
using AgendamentoWpfApp.Models;

namespace AgendamentoWpfApp.Services.Validation;

internal static class ConsultaValidator
{
    public static ValidationResult Validate(Consulta consulta)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(consulta.ClienteIdLocal))
            result.Add("Selecione um cliente cadastrado.");

        if (consulta.DataConsulta == default)
            result.Add("Informe o dia da consulta.");
        else if (consulta.DataConsulta.Date < DateTime.Today)
            result.Add("A consulta nao pode ser marcada em data passada.");

        if (string.IsNullOrWhiteSpace(consulta.Horario))
            result.Add("Informe um horario valido, como 14:00.");

        if (string.IsNullOrWhiteSpace(consulta.Local))
            result.Add("Informe o local da consulta.");

        return result;
    }
}
