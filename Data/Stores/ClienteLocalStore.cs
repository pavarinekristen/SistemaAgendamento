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

    public async Task SaveAsync(Cliente cliente)
    {
        await using var context = _database.CreateContext();
        cliente.Cpf = InputNormalizer.NormalizeCpf(cliente.Cpf);
        cliente.Telefone = InputNormalizer.NormalizeTelefone(cliente.Telefone);
        cliente.Email = InputNormalizer.NormalizeEmail(cliente.Email);

        var validation = ClienteValidator.Validate(cliente);
        if (!validation.IsValid)
            throw new System.InvalidOperationException(validation.Message);

        if (!string.IsNullOrWhiteSpace(cliente.Cpf))
        {
            var duplicado = await context.Clientes
                .AsNoTracking()
                .Where(c => !c.Excluido)
                .Where(c => c.IdLocal != cliente.IdLocal)
                .AnyAsync(c => c.Cpf == cliente.Cpf);

            if (duplicado)
                throw new System.InvalidOperationException("Ja existe cliente cadastrado com este CPF.");
        }

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
