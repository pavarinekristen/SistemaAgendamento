using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgendamentoWpfApp.Data;
using AgendamentoWpfApp.Models;
using AgendamentoWpfApp.Services.Validation;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoWpfApp.Data.Stores;

internal sealed class ClienteLocalStore
{
    private readonly AgendaDatabase _database;

    public ClienteLocalStore(string? databasePath = null)
    {
        _database = new AgendaDatabase(databasePath);
    }

    public Task MigrateAsync()
    {
        return _database.MigrateAsync();
    }

    public async Task<List<Cliente>> LoadAsync(bool incluirExcluidos = false)
    {
        await using var context = _database.CreateContext();
        var query = context.Clientes.AsNoTracking();

        if (!incluirExcluidos)
            query = query.Where(c => !c.Excluido);

        return await query
            .OrderBy(c => c.Nome)
            .ThenBy(c => c.Cpf)
            .ToListAsync();
    }

    public async Task<List<Cliente>> SearchAsync(string term, int take = 50, bool incluirExcluidos = false)
    {
        await using var context = _database.CreateContext();
        take = System.Math.Clamp(take <= 0 ? 50 : take, 10, 100);

        return await AplicarFiltros(context, term, empresa: "", incluirExcluidos)
            .OrderBy(c => c.Nome)
            .ThenBy(c => c.Cpf)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<Cliente>> SearchPageAsync(string term, string empresa, int skip, int take, bool incluirExcluidos = false)
    {
        await using var context = _database.CreateContext();
        skip = System.Math.Max(0, skip);
        take = System.Math.Clamp(take <= 0 ? 100 : take, 10, 500);

        return await AplicarFiltros(context, term, empresa, incluirExcluidos)
            .OrderBy(c => c.Nome)
            .ThenBy(c => c.Cpf)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountAsync(string term = "", string empresa = "", bool incluirExcluidos = false)
    {
        await using var context = _database.CreateContext();
        return await AplicarFiltros(context, term, empresa, incluirExcluidos).CountAsync();
    }

    public async Task<List<string>> ListEmpresasAsync()
    {
        // Valores exatos como estao no banco: o filtro de empresa compara por
        // igualdade, entao cada variacao de caixa/acento aparece na lista.
        await using var context = _database.CreateContext();
        return await context.Clientes.AsNoTracking()
            .Where(c => !c.Excluido && c.Empresa != null && c.Empresa != "")
            .Select(c => c.Empresa)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();
    }

    private static IQueryable<Cliente> AplicarFiltros(AgendaDbContext context, string term, string empresa, bool incluirExcluidos)
    {
        term = (term ?? string.Empty).Trim();
        // Empresa NAO trima: o valor vem do dropdown (direto do banco) e a
        // comparacao e por igualdade com o valor cru armazenado.
        empresa = empresa ?? string.Empty;

        var query = context.Clientes.AsNoTracking();

        if (!incluirExcluidos)
            query = query.Where(c => !c.Excluido);

        // Igualdade exata: o valor vem do proprio banco (dropdown de empresas),
        // e LIKE trataria % e _ do nome da empresa como curinga.
        if (!string.IsNullOrWhiteSpace(empresa))
            query = query.Where(c => c.Empresa == empresa);

        if (!string.IsNullOrWhiteSpace(term))
        {
            // Termo numerico tambem busca pelo ID do cliente (ex.: "12" ou "0012").
            var buscaPorId = int.TryParse(term, out var idBusca) && idBusca > 0;

            // Blob normalizado (caixa alta, sem acento) cobre nome, empresa,
            // cargo, CPF, RG, telefone, e-mail e status; curingas escapados
            // para "100%" ser literal.
            var padrao = $"%{EscaparLike(InputNormalizer.NormalizeSearchText(term))}%";
            query = query.Where(c =>
                (buscaPorId && c.Id == idBusca) ||
                EF.Functions.Like(c.PesquisaNormalizada, padrao, "\\"));
        }

        return query;
    }

    // Espelhado no backfill SQL de AgendaDatabase.EnsureClientePesquisaNormalizadaAsync;
    // quem alterar aqui precisa alterar la. A variante so-digitos cobre cadastro
    // legado com CPF/RG/telefone formatados ("123.456.789-01") quando o usuario
    // digita apenas os numeros.
    internal static string MontarPesquisaNormalizada(Cliente cliente)
    {
        return InputNormalizer.NormalizeSearchText(string.Join(
            "\n",
            cliente.Nome,
            cliente.Empresa,
            cliente.Cargo,
            cliente.Cpf,
            cliente.Rg,
            cliente.Telefone,
            cliente.Email,
            cliente.Status,
            InputNormalizer.OnlyDigits(cliente.Cpf),
            InputNormalizer.OnlyDigits(cliente.Rg),
            InputNormalizer.OnlyDigits(cliente.Telefone)));
    }

    private static string EscaparLike(string valor)
    {
        return (valor ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
    }

    public async Task<Cliente?> FindByIdLocalAsync(string idLocal, bool incluirExcluidos = false)
    {
        if (string.IsNullOrWhiteSpace(idLocal))
            return null;

        await using var context = _database.CreateContext();
        var query = context.Clientes.AsNoTracking().Where(c => c.IdLocal == idLocal);

        if (!incluirExcluidos)
            query = query.Where(c => !c.Excluido);

        return await query.FirstOrDefaultAsync();
    }

    public async Task SaveAsync(Cliente cliente)
    {
        await using var context = _database.CreateContext();
        cliente.Cpf = InputNormalizer.NormalizeCpf(cliente.Cpf);
        cliente.Telefone = InputNormalizer.NormalizeTelefone(cliente.Telefone);
        cliente.Email = InputNormalizer.NormalizeEmail(cliente.Email);

        var validation = ClienteValidator.Validate(cliente);
        if (!validation.IsValid)
            throw new System.InvalidOperationException(validation.Message);

        cliente.PesquisaNormalizada = MontarPesquisaNormalizada(cliente);
        cliente.AtualizadoEm = System.DateTime.Now;

        if (cliente.Id == 0)
        {
            await context.Clientes.AddAsync(cliente);
        }
        else
        {
            context.Clientes.Update(cliente);
        }

        await context.SaveChangesAsync();
    }

    public Task MarkDeletedAsync(Cliente cliente)
    {
        return ExclusaoLocalStore.MarcarExcluidoAsync(_database, cliente);
    }
}
