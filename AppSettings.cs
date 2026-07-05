using System;

namespace AgendamentoWpfApp;

internal static class AppSettings
{
    public const string ApiUrlEnvironmentVariable = "AGENDAMENTO_RETAGUARDA_URL";

    public static string ApiBaseUrl =>
        (Environment.GetEnvironmentVariable(ApiUrlEnvironmentVariable) ?? "http://localhost:5000").Trim().TrimEnd('/');
}
