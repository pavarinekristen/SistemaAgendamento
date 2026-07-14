using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AgendamentoWpfApp.Services.Validation;

internal static class InputNormalizer
{
    public static string OnlyDigits(string value)
    {
        return new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
    }

    public static string NormalizeCpf(string value)
    {
        var digits = OnlyDigits(value);
        return digits.Length > 11 ? digits[..11] : digits;
    }

    public static string NormalizeTelefone(string value)
    {
        var digits = OnlyDigits(value);
        return digits.Length > 11 ? digits[..11] : digits;
    }

    public static string NormalizeEmail(string value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant();
    }

    public static bool IsValidEmail(string value)
    {
        value = NormalizeEmail(value);
        return string.IsNullOrWhiteSpace(value) || Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    public static bool IsValidCpf(string value)
    {
        var cpf = NormalizeCpf(value);
        if (string.IsNullOrWhiteSpace(cpf))
            return true;

        if (cpf.Length != 11)
            return false;

        if (cpf.All(c => c == cpf[0]))
            return false;

        return CalculateCpfDigit(cpf[..9], 10) == cpf[9] - '0'
            && CalculateCpfDigit(cpf[..10], 11) == cpf[10] - '0';
    }

    public static string FormatCpf(string value)
    {
        var digits = NormalizeCpf(value);
        if (digits.Length <= 3)
            return digits;
        if (digits.Length <= 6)
            return $"{digits[..3]}.{digits[3..]}";
        if (digits.Length <= 9)
            return $"{digits[..3]}.{digits[3..6]}.{digits[6..]}";

        return $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}";
    }

    public static string FormatTelefone(string value)
    {
        var digits = NormalizeTelefone(value);
        if (digits.Length <= 2)
            return digits;
        if (digits.Length <= 6)
            return $"({digits[..2]}) {digits[2..]}";
        if (digits.Length <= 10)
            return $"({digits[..2]}) {digits[2..6]}-{digits[6..]}";

        return $"({digits[..2]}) {digits[2..7]}-{digits[7..]}";
    }

    // Base da pesquisa de clientes: caixa alta e sem acentos, para que "jose"
    // encontre "José" independente de como o cadastro foi digitado (o LIKE do
    // SQLite so ignora caixa ASCII e nao conhece acentos).
    public static string NormalizeSearchText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decomposto = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposto.Length);
        foreach (var ch in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToUpperInvariant(ch));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static bool TryNormalizeHorario(string value, out string horario)
    {
        horario = string.Empty;
        var text = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (TimeSpan.TryParse(text, CultureInfo.CurrentCulture, out var time) ||
            TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out time))
        {
            if (time.TotalHours >= 0 && time.TotalHours < 24)
            {
                horario = $"{(int)time.TotalHours:00}:{time.Minutes:00}";
                return true;
            }
        }

        var digits = OnlyDigits(text);
        if (digits.Length is 3 or 4 &&
            int.TryParse(digits[..^2], out var hour) &&
            int.TryParse(digits[^2..], out var minute) &&
            hour is >= 0 and < 24 &&
            minute is >= 0 and < 60)
        {
            horario = $"{hour:00}:{minute:00}";
            return true;
        }

        if (digits.Length <= 2 &&
            int.TryParse(digits, out hour) &&
            hour is >= 0 and < 24)
        {
            horario = $"{hour:00}:00";
            return true;
        }

        return false;
    }

    private static int CalculateCpfDigit(string digits, int initialWeight)
    {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
            sum += (digits[i] - '0') * (initialWeight - i);

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
