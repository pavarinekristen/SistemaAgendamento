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
        var snapshot = await CriarSnapshotAsync(completo);
        var lotes = DividirEmLotes(snapshot, MaxRegistrosPorLote);

        // "Ausentes = excluidos" so vale quando todas as linhas foram num unico
        // request; dividido em lotes, cada lote e parcial por definicao.
        if (completo && lotes.Count == 1)
            lotes[0].SnapshotCompleto = true;

        AgendaSnapshotResponse resultado = null!;
        var totalEnviado = 0;

        foreach (var lote in lotes)
        {
            resultado = await EnviarLoteAsync(lote);
            totalEnviado += lote.Tabelas.Sum(t => t.Registros.Count);

            // Marca por lote: se a conexao cair no meio, o que ja subiu nao e
            // reenviado na proxima tentativa (o upsert do servidor e idempotente).
            await MarcarRegistrosComoSincronizadosAsync(lote, inicioSnapshot);
        }

        resultado.TotalTabelas = snapshot.Tabelas.Count;
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

    internal static List<AgendaSnapshotRequest> DividirEmLotes(AgendaSnapshotRequest snapshot, int maxRegistros)
    {
        var lotes = new List<AgendaSnapshotRequest>();
        AgendaSnapshotRequest? atual = null;
        var capacidade = 0;

        foreach (var tabela in snapshot.Tabelas)
        {
            var offset = 0;
            do
            {
                if (atual == null || capacidade <= 0)
                {
                    atual = new AgendaSnapshotRequest
                    {
                        DispositivoId = snapshot.DispositivoId,
                        SnapshotCompleto = false
                    };
                    lotes.Add(atual);
                    capacidade = maxRegistros;
                }

                var quantidade = Math.Min(capacidade, tabela.Registros.Count - offset);
                atual.Tabelas.Add(new AgendaSnapshotTable
                {
                    Nome = tabela.Nome,
                    Registros = tabela.Registros.GetRange(offset, quantidade)
                });
                offset += quantidade;
                capacidade -= quantidade;
            } while (offset < tabela.Registros.Count);
        }

        if (lotes.Count == 0)
            lotes.Add(new AgendaSnapshotRequest
            {
                DispositivoId = snapshot.DispositivoId,
                SnapshotCompleto = snapshot.SnapshotCompleto
            });

        return lotes;
    }

    internal async Task<AgendaSnapshotRequest> CriarSnapshotAsync(bool completo)
    {
        var snapshot = new AgendaSnapshotRequest
        {
            DispositivoId = Environment.MachineName,
            SnapshotCompleto = completo
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
                var colunas = await ListarColunasSqliteAsync(connection, tableName);
                var incremental = !completo
                    && colunas.Contains("AtualizadoEm")
                    && colunas.Contains("SincronizadoEm");

                var tabela = new AgendaSnapshotTable { Nome = tableName };
                await using var command = connection.CreateCommand();
                var nomeEscapado = EscaparIdentificadorSqlite(tableName);
                command.CommandText = incremental
                    ? $"SELECT * FROM \"{nomeEscapado}\" WHERE \"SincronizadoEm\" IS NULL OR \"AtualizadoEm\" > \"SincronizadoEm\""
                    : $"SELECT * FROM \"{nomeEscapado}\"";
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

    internal async Task MarcarRegistrosComoSincronizadosAsync(AgendaSnapshotRequest lote, DateTime inicioSnapshot)
    {
        await using var context = _database.CreateContext();
        var agora = DateTime.Now;

        foreach (var tabela in lote.Tabelas)
        {
            if (tabela.Registros.Count == 0)
                continue;

            var ids = tabela.Registros.Select(r => r.IdLocal).ToList();

            // Marca apenas o que foi enviado neste lote e nao mudou desde a montagem
            // do snapshot; edicao durante o envio (AtualizadoEm > inicio) segue pendente.
            if (tabela.Nome.Equals("CLIENTES", StringComparison.OrdinalIgnoreCase))
                await context.Clientes
                    .Where(c => ids.Contains(c.IdLocal) && c.AtualizadoEm <= inicioSnapshot)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.SincronizadoEm, agora));
            else if (tabela.Nome.Equals("CONSULTAS", StringComparison.OrdinalIgnoreCase))
                await context.Consultas
                    .Where(c => ids.Contains(c.IdLocal) && c.AtualizadoEm <= inicioSnapshot)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.SincronizadoEm, agora));
            else if (tabela.Nome.Equals("PROFISSIONAIS_SALAS", StringComparison.OrdinalIgnoreCase))
                await context.ProfissionaisSalas
                    .Where(p => ids.Contains(p.IdLocal) && p.AtualizadoEm <= inicioSnapshot)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.SincronizadoEm, agora));
        }
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
