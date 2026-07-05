using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AgendamentoWpfApp.Data;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoWpfApp.Services;

internal sealed class AgendaSnapshotSyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AgendaDatabase _database;

    public AgendaSnapshotSyncService(string? databasePath = null)
    {
        _database = new AgendaDatabase(databasePath);
    }

    public async Task<AgendaSnapshotResponse> SincronizarAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionState.BaseUrl) || string.IsNullOrWhiteSpace(SessionState.Token))
            throw new InvalidOperationException("Sessao da API nao esta ativa.");

        await _database.MigrateAsync();
        var snapshot = await CriarSnapshotAsync();

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(SessionState.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(60)
        };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionState.Token);

        var response = await httpClient.PostAsJsonAsync("sincroniza/agenda/snapshot", snapshot, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Falha na sincronizacao: {(int)response.StatusCode} {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<AgendaSnapshotResponse>(JsonOptions)
            ?? new AgendaSnapshotResponse();

        await MarcarRegistrosComoSincronizadosAsync();
        return result;
    }

    private async Task<AgendaSnapshotRequest> CriarSnapshotAsync()
    {
        var snapshot = new AgendaSnapshotRequest
        {
            DispositivoId = Environment.MachineName
        };

        await using var context = _database.CreateContext();
        var connection = context.Database.GetDbConnection();
        var deveFechar = connection.State != ConnectionState.Open;
        if (deveFechar)
            await connection.OpenAsync();

        try
        {
            foreach (var tableName in ListarTabelasSqlite(connection))
            {
                var tabela = new AgendaSnapshotTable { Nome = tableName };
                await using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM \"{EscaparIdentificadorSqlite(tableName)}\"";
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var registro = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < reader.FieldCount; i++)
                        registro[reader.GetName(i)] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);

                    var dadosJson = JsonSerializer.Serialize(registro, JsonOptions);
                    tabela.Registros.Add(new AgendaSnapshotRecord
                    {
                        IdLocal = ObterIdLocal(registro, dadosJson),
                        DadosJson = dadosJson,
                        Hash = Hash(dadosJson)
                    });
                }

                snapshot.Tabelas.Add(tabela);
            }
        }
        finally
        {
            if (deveFechar)
                await connection.CloseAsync();
        }

        return snapshot;
    }

    private async Task MarcarRegistrosComoSincronizadosAsync()
    {
        await using var context = _database.CreateContext();
        var agora = DateTime.Now;

        var clientes = await context.Clientes.ToListAsync();
        foreach (var cliente in clientes)
            cliente.SincronizadoEm = agora;

        var consultas = await context.Consultas.ToListAsync();
        foreach (var consulta in consultas)
            consulta.SincronizadoEm = agora;

        var profissionaisSalas = await context.ProfissionaisSalas.ToListAsync();
        foreach (var profissionalSala in profissionaisSalas)
            profissionalSala.SincronizadoEm = agora;

        await context.SaveChangesAsync();
    }

    private static IReadOnlyList<string> ListarTabelasSqlite(System.Data.Common.DbConnection connection)
    {
        var tabelas = new List<string>();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT name
              FROM sqlite_master
             WHERE type = 'table'
               AND name NOT LIKE 'sqlite_%'
               AND name <> '__EFMigrationsHistory'
             ORDER BY name";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var nome = reader.GetString(0);
            if (!string.IsNullOrWhiteSpace(nome))
                tabelas.Add(nome);
        }

        return tabelas;
    }

    private static string ObterIdLocal(Dictionary<string, object?> registro, string dadosJson)
    {
        if (registro.TryGetValue("IdLocal", out var idLocal) && !string.IsNullOrWhiteSpace(idLocal?.ToString()))
            return idLocal.ToString()!;

        if (registro.TryGetValue("Id", out var id) || registro.TryGetValue("ID", out id))
            return id?.ToString() ?? Hash(dadosJson);

        return Hash(dadosJson);
    }

    private static string EscaparIdentificadorSqlite(string value)
    {
        return (value ?? string.Empty).Replace("\"", "\"\"");
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

internal sealed class AgendaSnapshotRequest
{
    [JsonPropertyName("dispositivoId")]
    public string DispositivoId { get; set; } = string.Empty;

    [JsonPropertyName("tabelas")]
    public List<AgendaSnapshotTable> Tabelas { get; set; } = new();
}

internal sealed class AgendaSnapshotTable
{
    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("registros")]
    public List<AgendaSnapshotRecord> Registros { get; set; } = new();
}

internal sealed class AgendaSnapshotRecord
{
    [JsonPropertyName("idLocal")]
    public string IdLocal { get; set; } = string.Empty;

    [JsonPropertyName("dadosJson")]
    public string DadosJson { get; set; } = string.Empty;

    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;
}

internal sealed class AgendaSnapshotResponse
{
    [JsonPropertyName("bancoOperacional")]
    public string BancoOperacional { get; set; } = string.Empty;

    [JsonPropertyName("totalTabelas")]
    public int TotalTabelas { get; set; }

    [JsonPropertyName("totalRegistros")]
    public int TotalRegistros { get; set; }
}
