namespace AgendamentoWpfApp;

internal static class SessionState
{
    public static string BaseUrl { get; set; } = string.Empty;
    public static string Token { get; set; } = string.Empty;
    public static string UsuarioNome { get; set; } = string.Empty;
    public static string EmpresaNome { get; set; } = string.Empty;

    public static void Clear()
    {
        BaseUrl = string.Empty;
        Token = string.Empty;
        UsuarioNome = string.Empty;
        EmpresaNome = string.Empty;
    }
}
