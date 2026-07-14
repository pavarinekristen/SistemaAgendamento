using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AgendamentoWpfApp.Data;
using AgendamentoWpfApp.Models;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoWpfApp.Services;

internal sealed class AgendaSnapshotSyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HttpClient SharedHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    private readonly AgendaDatabase _database;

    public AgendaSnapshotSyncService(string? databasePath = null)
    {
        _database = new AgendaDatabase(databasePath);
    }

    // Limite de registros por requisicao: mantem o payload em ~1-2 MB, abaixo do
    // limite de body do servidor e do timeout, mesmo com dezenas de milhares pendentes.
    internal const int MaxRegistrosPorLote = 1000;

    public async Task<AgendaSnapshotResponse> SincronizarAsync(bool completo = false)
    {
        if (string.IsNullOrWhiteSpace(SessionState.BaseUrl) || string.IsNullOrWhiteSpace(SessionState.Token))
            throw new InvalidOperationException("Sessao da API nao esta ativa.");

        await _database.MigrateAsync();

        // Registros alterados depois deste instante ficam para o proximo sync:
        // a marcacao de sincronizado so alcanca AtualizadoEm <= inicioSnapshot.
        var inicioSnapshot = DateTime.Now;

        var resultado = new AgendaSnapshotResponse();
        var totalEnviado = 0;
        var tabelasEnviadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Lotes montados sob demanda direto do banco: o snapshot inteiro nunca
        // fica em memoria (na primeira carga sao ~48k registros). Marca por
        // lote: se a conexao cair no meio, o que ja subiu nao e reenviado na
        // proxima tentativa (o upsert do servidor e idempotente).
        await foreach (var lote in CriarLotesAsync(completo, MaxRegistrosPorLote))
        {
            resultado = await EnviarLoteAsync(lote);

            foreach (var tabela in lote.Tabelas)
            {
                tabelasEnviadas.Add(tabela.Nome);
                totalEnviado += tabela.Registros.Count;
            }

            await MarcarRegistrosComoSincronizadosAsync(lote, inicioSnapshot);
        }

        resultado.TotalTabelas = tabelasEnviadas.Count;
        resultado.TotalRegistros = totalEnviado;
        return resultado;
    }

    private static async Task<AgendaSnapshotResponse> EnviarLoteAsync(AgendaSnapshotRequest lote)
    {
        var baseUri = new Uri(SessionState.BaseUrl.TrimEnd('/') + "/");
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "sincroniza/agenda/snapshot"))
        {
            Content = JsonContent.Create(lote, options: JsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SessionState.Token);

        using var response = await SharedHttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Falha na sincronizacao: {(int)response.StatusCode} {body}");
        }

        return await response.Content.ReadFromJsonAsync<AgendaSnapshotResponse>(JsonOptions)
            ?? new AgendaSnapshotResponse();
    }

    internal async IAsyncEnumerable<AgendaSnapshotRequest> CriarLotesAsync(bool completo, int maxRegistros)
    {
        maxRegistros = Math.Max(1, maxRegistros);
        var dispositivoId = Environment.MachineName;
        var gerouLote = false;

        await using var context = _database.CreateContext();
        var connection = context.Database.GetDbConnection();
        var deveFechar = connection.State != ConnectionState.Open;
        if (deveFechar)
            await connection.OpenAsync();

        try
        {
            // Nenhum lote afirma "snapshot completo": dividido em lotes, cada
            // request e parcial por definicao, e a regra "ausentes = excluidos"
            // do servidor marcaria como excluido o que ficou nos outros lotes.
            // Exclusoes chegam pelo proprio payload (campo Excluido do registro).
            AgendaSnapshotRequest? atual = null;
            AgendaSnapshotTable? tabelaAtual = null;
            var capacidade = 0;

            foreach (var tableName in ListarTabelasSqlite(connection))
            {
                var colunas = await ListarColunasSqliteAsync(connection, tableName);
                var incremental = !completo
                    && colunas.Contains("AtualizadoEm")
                    && colunas.Contains("SincronizadoEm");

                await using var command = connection.CreateCommand();
                var nomeEscapado = EscaparIdentificadorSqlite(tableName);
                command.CommandText = incremental
                    ? $"SELECT * FROM \"{nomeEscapado}\" WHERE \"SincronizadoEm\" IS NULL OR \"AtualizadoEm\" > \"SincronizadoEm\""
                    : $"SELECT * FROM \"{nomeEscapado}\"";
                await using var reader = await command.ExecuteReaderAsync();

                tabelaAtual = null;
                while (await reader.ReadAsync())
                {
                    var registro = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < reader.FieldCount; i++)
                        registro[reader.GetName(i)] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);

                    var dadosJson = JsonSerializer.Serialize(registro, JsonOptions);

                    if (atual == null)
                    {
                        atual = new AgendaSnapshotRequest { DispositivoId = dispositivoId };
                        capacidade = maxRegistros;
                        tabelaAtual = null;
                    }

                    if (tabelaAtual == null)
                    {
                        tabelaAtual = new AgendaSnapshotTable { Nome = tableName };
                        atual.Tabelas.Add(tabelaAtual);
                    }

                    tabelaAtual.Registros.Add(new AgendaSnapshotRecord
                    {
                        IdLocal = ObterIdLocal(registro, dadosJson),
                        DadosJson = dadosJson,
                        Hash = Hash(dadosJson)
                    });

                    if (--capacidade == 0)
                    {
                        gerouLote = true;
                        yield return atual;
                        atual = null;
                        tabelaAtual = null;
                    }
                }
            }

            if (atual != null)
            {
                gerouLote = true;
                yield return atual;
            }

            // Nada pendente: um request vazio ainda valida a sessao e registra
            // o dispositivo (e nunca afirma snapshot completo: um "completo"
            // vazio autorizaria o servidor a excluir tudo do dispositivo).
            if (!gerouLote)
                yield return new AgendaSnapshotRequest { DispositivoId = dispositivoId };
        }
        finally
        {
            if (deveFechar)
                await connection.CloseAsync();
        }
    }

    internal async Task MarcarRegistrosComoSincronizadosAsync(AgendaSnapshotRequest lote, DateTime inicioSnapshot)
    {
        await using var context = _database.CreateContext();
        var agora = DateTime.Now;

        foreach (var tabela in lote.Tabelas)
        {
            if (tabela.Registros.Count == 0)
                continue;

            var ids = tabela.Registros.Select(r => r.IdLocal).ToList();
            await (tabela.Nome.ToUpperInvariant() switch
            {
                "CLIENTES" => MarcarTabelaAsync(context.Clientes, ids, inicioSnapshot, agora),
                "CONSULTAS" => MarcarTabelaAsync(context.Consultas, ids, inicioSnapshot, agora),
                "PROFISSIONAIS_SALAS" => MarcarTabelaAsync(context.ProfissionaisSalas, ids, inicioSnapshot, agora),
                _ => Task.CompletedTask
            });
        }
    }

    // Regra unica de marcacao para todas as tabelas sincronizadas: apenas o que
    // foi enviado neste lote e nao mudou desde a montagem do snapshot; edicao
    // durante o envio (AtualizadoEm > inicioSnapshot) segue pendente.
    private static Task MarcarTabelaAsync<T>(DbSet<T> registros, List<string> ids, DateTime inicioSnapshot, DateTime agora)
        where T : class, IRegistroSincronizavel
    {
        return registros
            .Where(r => ids.Contains(r.IdLocal) && r.AtualizadoEm <= inicioSnapshot)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.SincronizadoEm, agora));
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

    private static async Task<HashSet<string>> ListarColunasSqliteAsync(System.Data.Common.DbConnection connection, string tableName)
    {
        var colunas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{EscaparIdentificadorSqlite(tableName)}\")";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            colunas.Add(reader.GetString(1));

        return colunas;
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

    [JsonPropertyName("snapshotCompleto")]
    public bool SnapshotCompleto { get; set; }

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
