using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgendamentoWpfApp.Services;

internal sealed class AuthApiService
{
    private readonly HttpClient _httpClient = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ApiOperationResult<AuthResponse>> LoginAsync(string email, string senha)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{AppSettings.ApiBaseUrl}/auth/login",
                new LoginRequest(email, senha));

            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                return ApiOperationResult<AuthResponse>.Fail(TryReadApiError(body) ?? "Nao foi possivel entrar. Verifique os dados e tente novamente.");

            var auth = JsonSerializer.Deserialize<AuthResponse>(body, _jsonOptions);
            if (auth == null)
                return ApiOperationResult<AuthResponse>.Fail("A API retornou uma resposta vazia.");

            if (string.IsNullOrWhiteSpace(auth.Token))
                return ApiOperationResult<AuthResponse>.Fail("Conta pendente de confirmacao. Confirme o codigo enviado por e-mail antes de entrar.");

            return ApiOperationResult<AuthResponse>.Ok(auth);
        }
        catch (HttpRequestException)
        {
            return ApiOperationResult<AuthResponse>.Fail($"Nao foi possivel conectar na API. Confirme se ela esta rodando em {AppSettings.ApiBaseUrl}.");
        }
        catch (TaskCanceledException)
        {
            return ApiOperationResult<AuthResponse>.Fail("Tempo de conexao esgotado.");
        }
        catch (Exception ex)
        {
            return ApiOperationResult<AuthResponse>.Fail($"Erro inesperado: {ex.Message}");
        }
    }

    public async Task<ApiOperationResult> CreateAccountAsync(CreateAccountRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{AppSettings.ApiBaseUrl}/auth/criar-conta", request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return ApiOperationResult.Fail(TryReadApiError(body) ?? "Nao foi possivel criar a conta.");

            return ApiOperationResult.Ok();
        }
        catch (HttpRequestException)
        {
            return ApiOperationResult.Fail($"Nao foi possivel conectar na API em {AppSettings.ApiBaseUrl}.");
        }
        catch (Exception ex)
        {
            return ApiOperationResult.Fail($"Erro inesperado: {ex.Message}");
        }
    }

    public async Task<ApiOperationResult> ConfirmEmailAsync(string email, string code)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{AppSettings.ApiBaseUrl}/auth/confirmar-email",
                new ConfirmEmailRequest(email, code));

            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                return ApiOperationResult.Fail(TryReadApiError(body) ?? "Nao foi possivel confirmar o e-mail.");

            return ApiOperationResult.Ok();
        }
        catch (HttpRequestException)
        {
            return ApiOperationResult.Fail($"Nao foi possivel conectar na API em {AppSettings.ApiBaseUrl}.");
        }
        catch (Exception ex)
        {
            return ApiOperationResult.Fail($"Erro inesperado: {ex.Message}");
        }
    }

    private string? TryReadApiError(string body)
    {
        try
        {
            var error = JsonSerializer.Deserialize<ApiErrorResponse>(body, _jsonOptions);
            return string.IsNullOrWhiteSpace(error?.Message) ? null : error.Message;
        }
        catch
        {
            return null;
        }
    }
}

internal class ApiOperationResult
{
    protected ApiOperationResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public bool Success { get; }
    public string Message { get; }

    public static ApiOperationResult Ok()
    {
        return new ApiOperationResult(true, "");
    }

    public static ApiOperationResult Fail(string message)
    {
        return new ApiOperationResult(false, message);
    }
}

internal sealed class ApiOperationResult<T> : ApiOperationResult
{
    private ApiOperationResult(bool success, string message, T? value)
        : base(success, message)
    {
        Value = value;
    }

    public T? Value { get; }

    public static ApiOperationResult<T> Ok(T value)
    {
        return new ApiOperationResult<T>(true, "", value);
    }

    public new static ApiOperationResult<T> Fail(string message)
    {
        return new ApiOperationResult<T>(false, message, default);
    }
}

internal sealed record LoginRequest(string Email, string Senha);
internal sealed record ConfirmEmailRequest(string Email, string Codigo);

internal sealed class CreateAccountRequest
{
    public string Cnpj { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UsuarioNome { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
}

internal sealed class AuthResponse
{
    public string? Token { get; set; }
    public DateTime ExpiraEm { get; set; }
    public AuthUsuarioResponse? Usuario { get; set; }
    public AuthEmpresaResponse? Empresa { get; set; }
}

internal sealed class AuthUsuarioResponse
{
    public string? Nome { get; set; }
    public string? Email { get; set; }
}

internal sealed class AuthEmpresaResponse
{
    public string? RazaoSocial { get; set; }
    public string? NomeFantasia { get; set; }
}

internal sealed class ApiErrorResponse
{
    public string? Message { get; set; }
}
