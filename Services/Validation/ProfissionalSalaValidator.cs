using AgendamentoWpfApp.Models;

namespace AgendamentoWpfApp.Services.Validation;

internal static class ProfissionalSalaValidator
{
    public static ValidationResult Validate(ProfissionalSala profissionalSala)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(profissionalSala.Nome))
            result.Add("Informe o nome.");

        if (string.IsNullOrWhiteSpace(profissionalSala.Tipo))
            result.Add("Informe o tipo.");

        if (!InputNormalizer.IsValidEmail(profissionalSala.Email))
            result.Add("Informe um e-mail valido ou deixe em branco.");

        return result;
    }
}
