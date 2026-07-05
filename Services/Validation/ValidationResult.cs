using System.Collections.Generic;
using System.Linq;

namespace AgendamentoWpfApp.Services.Validation;

internal sealed class ValidationResult
{
    private readonly List<string> _errors = new();

    public bool IsValid => _errors.Count == 0;
    public IReadOnlyList<string> Errors => _errors;
    public string Message => string.Join("\n", _errors);

    public void Add(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            _errors.Add(message.Trim());
    }

    public void AddRange(IEnumerable<string> messages)
    {
        foreach (var message in messages.Where(m => !string.IsNullOrWhiteSpace(m)))
            Add(message);
    }
}
