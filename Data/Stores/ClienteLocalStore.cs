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
        await using var context = _database.CreateContext();
        var empresas = await context.Clientes.AsNoTracking()
            .Where(c => !c.Excluido && c.Empresa != null && c.Empresa != "")
            .Select(c => c.Empresa)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();

        // O Distinct do SQLite diferencia maiusculas; a lista final nao deve.
        return empresas
            .Distinct(System.StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IQueryable<Cliente> AplicarFiltros(AgendaDbContext context, string term, string empresa, bool incluirExcluidos)
    {
        term = (term ?? string.Empty).Trim();
        empresa = (empresa ?? string.Empty).Trim();

        var query = context.Clientes.AsNoTracking();

        if (!incluirExcluidos)
            query = query.Where(c => !c.Excluido);

        // LIKE sem curinga: igualdade sem diferenciar maiusculas/minusculas.
        if (!string.IsNullOrWhiteSpace(empresa))
            query = query.Where(c => EF.Functions.Like(c.Empresa, empresa));

        if (!string.IsNullOrWhiteSpace(term))
        {
            // Termo numerico tambem busca pelo ID do cliente (ex.: "12" ou "0012").
            var buscaPorId = int.TryParse(term, out var idBusca) && idBusca > 0;

            query = query.Where(c =>
                (buscaPorId && c.Id == idBusca) ||
                EF.Functions.Like(c.Nome, $"%{term}%") ||
                EF.Functions.Like(c.Empresa, $"%{term}%") ||
                EF.Functions.Like(c.Cargo, $"%{term}%") ||
                EF.Functions.Like(c.Cpf, $"%{term}%") ||
                EF.Functions.Like(c.Rg, $"%{term}%") ||
                EF.Functions.Like(c.Telefone, $"%{term}%") ||
                EF.Functions.Like(c.Email, $"%{term}%") ||
                EF.Functions.Like(c.Status, $"%{term}%"));
        }

        return query;
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

    public async Task SaveAllAsync(IEnumerable<Cliente> clientes)
    {
        await using var context = _database.CreateContext();
        context.Clientes.UpdateRange(clientes);
        await context.SaveChangesAsync();
    }

    public async Task MarkDeletedAsync(Cliente cliente)
    {
        cliente.Excluido = true;
        await SaveAsync(cliente);
    }
}
