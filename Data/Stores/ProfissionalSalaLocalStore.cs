using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgendamentoWpfApp.Data;
using AgendamentoWpfApp.Models;
using AgendamentoWpfApp.Services.Validation;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoWpfApp.Data.Stores;

internal sealed class ProfissionalSalaLocalStore
{
    private readonly AgendaDatabase _database;

    public ProfissionalSalaLocalStore(string? databasePath = null)
    {
        _database = new AgendaDatabase(databasePath);
    }

    public Task MigrateAsync()
    {
        return _database.MigrateAsync();
    }

    public async Task<List<ProfissionalSala>> LoadAsync(bool incluirExcluidos = false)
    {
        await using var context = _database.CreateContext();
        var query = context.ProfissionaisSalas.AsNoTracking();

        if (!incluirExcluidos)
            query = query.Where(p => !p.Excluido);

        return await query
            .OrderByDescending(p => p.Ativo)
            .ThenBy(p => p.Tipo)
            .ThenBy(p => p.Nome)
            .ToListAsync();
    }

    public async Task SaveAsync(ProfissionalSala profissionalSala)
    {
        await using var context = _database.CreateContext();
        profissionalSala.Telefone = InputNormalizer.NormalizeTelefone(profissionalSala.Telefone);
        profissionalSala.Email = InputNormalizer.NormalizeEmail(profissionalSala.Email);

        var validation = ProfissionalSalaValidator.Validate(profissionalSala);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.Message);

        profissionalSala.AtualizadoEm = DateTime.Now;

        if (profissionalSala.Id == 0)
            await context.ProfissionaisSalas.AddAsync(profissionalSala);
        else
            context.ProfissionaisSalas.Update(profissionalSala);

        await context.SaveChangesAsync();
    }

    public Task MarkDeletedAsync(ProfissionalSala profissionalSala)
    {
        return ExclusaoLocalStore.MarcarExcluidoAsync(_database, profissionalSala);
    }
}
