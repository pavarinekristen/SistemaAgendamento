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
        term = (term ?? string.Empty).Trim();
        take = System.Math.Clamp(take <= 0 ? 50 : take, 10, 100);

        var query = context.Clientes.AsNoTracking();

        if (!incluirExcluidos)
            query = query.Where(c => !c.Excluido);

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
                EF.Functions.Like(c.Rg, $"%{term}%"));
        }

        return await query
            .OrderBy(c => c.Nome)
            .ThenBy(c => c.Cpf)
            .Take(take)
            .ToListAsync();
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
