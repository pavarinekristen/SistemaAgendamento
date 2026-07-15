using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AgendamentoWpfApp.Services.Validation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoWpfApp.Data;

internal sealed class AgendaDatabase
{
    private readonly string _databasePath;

    public AgendaDatabase(string? databasePath = null)
    {
        _databasePath = string.IsNullOrWhiteSpace(databasePath)
            ? AgendaDbContext.DefaultDatabasePath
            : databasePath;

        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
    }

    public AgendaDbContext CreateContext()
    {
        return new AgendaDbContext(_databasePath);
    }

    public async Task MigrateAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        // WAL e persistente no arquivo: leitores nao bloqueiam o escritor, evitando
        // "database is locked" quando o sync automatico roda junto com a tela.
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
        await EnsureConsultaLaudoColumnsAsync(context);
        await EnsureClientePesquisaNormalizadaAsync(context);
        // Atende o ORDER BY Nome, Cpf da grade paginada de clientes sem B-tree
        // temporario sobre as ~48k linhas. Nao inclui Excluido: o EF gera
        // "WHERE NOT Excluido", que o planner do SQLite nao usa como igualdade.
        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_CLIENTES_Nome_Cpf ON CLIENTES (Nome, Cpf)");
    }

    private static async Task EnsureClientePesquisaNormalizadaAsync(AgendaDbContext context)
    {
        await context.Database.OpenConnectionAsync();

        try
        {
            var columns = await GetTableColumnsAsync(context, "CLIENTES");
            if (columns.Count == 0)
                return;

            if (!columns.Contains("PesquisaNormalizada"))
                await context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE CLIENTES ADD COLUMN PesquisaNormalizada TEXT NOT NULL DEFAULT ''");

            // Backfill dos cadastros antigos com a MESMA normalizacao do C#
            // (o SQLite nao sabe remover acentos); roda uma vez por linha vazia.
            // Espelhado em ClienteLocalStore.MontarPesquisaNormalizada: mesmas
            // colunas, mesma ordem, mesmas variantes so-digitos.
            if (context.Database.GetDbConnection() is SqliteConnection conexao)
            {
                conexao.CreateFunction(
                    "SPARK_NORMALIZAR_BUSCA",
                    (string texto) => InputNormalizer.NormalizeSearchText(texto ?? string.Empty));
                conexao.CreateFunction(
                    "SPARK_SOMENTE_DIGITOS",
                    (string texto) => InputNormalizer.OnlyDigits(texto ?? string.Empty));

                await context.Database.ExecuteSqlRawAsync(@"
                    UPDATE CLIENTES
                       SET PesquisaNormalizada = SPARK_NORMALIZAR_BUSCA(
                           COALESCE(Nome,'') || char(10) || COALESCE(Empresa,'') || char(10) ||
                           COALESCE(Cargo,'') || char(10) || COALESCE(Cpf,'') || char(10) ||
                           COALESCE(Rg,'') || char(10) || COALESCE(Telefone,'') || char(10) ||
                           COALESCE(Email,'') || char(10) || COALESCE(Status,'') || char(10) ||
                           SPARK_SOMENTE_DIGITOS(COALESCE(Cpf,'')) || char(10) ||
                           SPARK_SOMENTE_DIGITOS(COALESCE(Rg,'')) || char(10) ||
                           SPARK_SOMENTE_DIGITOS(COALESCE(Telefone,'')))
                     WHERE PesquisaNormalizada = ''");
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static async Task EnsureConsultaLaudoColumnsAsync(AgendaDbContext context)
    {
        await context.Database.OpenConnectionAsync();

        try
        {
            var columns = await GetTableColumnsAsync(context, "CONSULTAS");
            if (columns.Count == 0)
                return;

            if (!columns.Contains("LaudoGeradoEm"))
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE CONSULTAS ADD COLUMN LaudoGeradoEm TEXT");

            if (!columns.Contains("LaudoPdfNomeArquivo"))
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE CONSULTAS ADD COLUMN LaudoPdfNomeArquivo TEXT NOT NULL DEFAULT ''");

            if (!columns.Contains("LaudoPdfPath"))
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE CONSULTAS ADD COLUMN LaudoPdfPath TEXT NOT NULL DEFAULT ''");

            if (!columns.Contains("LaudoPdfUri"))
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE CONSULTAS ADD COLUMN LaudoPdfUri TEXT NOT NULL DEFAULT ''");

            if (!columns.Contains("LaudoTipo"))
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE CONSULTAS ADD COLUMN LaudoTipo TEXT NOT NULL DEFAULT ''");

            await context.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IF NOT EXISTS IX_CONSULTAS_LaudoGeradoEm ON CONSULTAS (LaudoGeradoEm)");
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static async Task<HashSet<string>> GetTableColumnsAsync(AgendaDbContext context, string tableName)
    {
        var columns = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName})";

        await using (command)
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                columns.Add(reader.GetString(1));
        }

        return columns;
    }
}
