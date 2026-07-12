using System;
using AgendamentoWpfApp.Models;

namespace AgendamentoWpfApp.Services.Validation;

internal static class ClienteValidator
{
    public static ValidationResult Validate(Cliente cliente)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(cliente.Nome))
            result.Add("Informe o nome do cliente.");

        if (string.IsNullOrWhiteSpace(cliente.Empresa))
            result.Add("Informe a empresa do funcionario.");

        if (string.IsNullOrWhiteSpace(cliente.Cargo))
            result.Add("Informe o cargo do funcionario.");

        if (!string.IsNullOrWhiteSpace(cliente.Cpf) && !InputNormalizer.IsValidCpf(cliente.Cpf))
            result.Add("CPF invalido.");

        var telefone = InputNormalizer.NormalizeTelefone(cliente.Telefone);
        if (!string.IsNullOrWhiteSpace(telefone) && telefone.Length is < 10 or > 11)
            result.Add("Telefone deve ter DDD e 10 ou 11 digitos.");

        if (!InputNormalizer.IsValidEmail(cliente.Email))
            result.Add("Informe um e-mail valido ou deixe em branco.");

        if (cliente.DataNascimento.HasValue && cliente.DataNascimento.Value.Date > DateTime.Today)
            result.Add("Nascimento nao pode ser uma data futura.");

        return result;
    }
}
