using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgendamentoWpfApp.Data;
using AgendamentoWpfApp.Models;
using AgendamentoWpfApp.Services.Validation;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoWpfApp.Data.Stores;

internal sealed class ConsultaLocalStore
{
    private readonly AgendaDatabase _database;

    public ConsultaLocalStore(string? databasePath = null)
    {
        _database = new AgendaDatabase(databasePath);
    }

    public Task MigrateAsync()
    {
        return _database.MigrateAsync();
    }

    public async Task<List<Consulta>> LoadByDateAsync(DateTime data, bool incluirExcluidas = false)
    {
        await using var context = _database.CreateContext();
        var dia = data.Date;
        var proximoDia = dia.AddDays(1);
        var query = context.Consultas.AsNoTracking().Where(c => c.DataConsulta >= dia && c.DataConsulta < proximoDia);

        if (!incluirExcluidas)
            query = query.Where(c => !c.Excluido);

        return await query
            .OrderBy(c => c.Horario)
            .ThenBy(c => c.ClienteNome)
            .ToListAsync();
    }

    public async Task<List<Consulta>> LoadUpcomingAsync(DateTime from, int take = 20)
    {
        await using var context = _database.CreateContext();
        return await context.Consultas
            .AsNoTracking()
            .Where(c => !c.Excluido && c.DataConsulta >= from.Date)
            .OrderBy(c => c.DataConsulta)
            .ThenBy(c => c.Horario)
            .ThenBy(c => c.ClienteNome)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<Consulta>> LoadByClienteAsync(string clienteIdLocal, bool incluirExcluidas = false)
    {
        await using var context = _database.CreateContext();
        var query = context.Consultas.AsNoTracking()
            .Where(c => c.ClienteIdLocal == clienteIdLocal);

        if (!incluirExcluidas)
            query = query.Where(c => !c.Excluido);

        return await query
            .OrderByDescending(c => c.DataConsulta)
            .ThenBy(c => c.Horario)
            .ToListAsync();
    }

    public async Task<List<Consulta>> LoadReportAsync(
        DateTime? dataInicial,
        DateTime? dataFinal,
        string empresa = "",
        string funcionario = "",
        string motivo = "",
        int take = 1000,
        bool incluirExcluidas = false)
    {
        await using var context = _database.CreateContext();
        var query = context.Consultas.AsNoTracking();

        if (!incluirExcluidas)
            query = query.Where(c => !c.Excluido);

        if (dataInicial.HasValue)
            query = query.Where(c => c.DataConsulta >= dataInicial.Value.Date);

        if (dataFinal.HasValue)
            query = query.Where(c => c.DataConsulta < dataFinal.Value.Date.AddDays(1));

        empresa = (empresa ?? string.Empty).Trim();
        funcionario = (funcionario ?? string.Empty).Trim();
        motivo = (motivo ?? string.Empty).Trim();

        if (!string.IsNullOrWhiteSpace(empresa))
            query = query.Where(c => EF.Functions.Like(c.Empresa, $"%{empresa}%"));

        if (!string.IsNullOrWhiteSpace(funcionario))
            query = query.Where(c =>
                EF.Functions.Like(c.ClienteNome, $"%{funcionario}%") ||
                EF.Functions.Like(c.ClienteCargo, $"%{funcionario}%"));

        if (!string.IsNullOrWhiteSpace(motivo))
            query = query.Where(c => c.Motivo == motivo);

        take = Math.Clamp(take <= 0 ? 1000 : take, 50, 2000);

        return await query
            .OrderByDescending(c => c.DataConsulta)
            .ThenBy(c => c.Horario)
            .ThenBy(c => c.Empresa)
            .ThenBy(c => c.ClienteNome)
            .Take(take)
            .ToListAsync();
    }

    public async Task SaveAsync(Consulta consulta)
    {
        await using var context = _database.CreateContext();
        var validation = ConsultaValidator.Validate(consulta);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.Message);

        var conflito = await context.Consultas
            .AsNoTracking()
            .Where(c => !c.Excluido)
            .Where(c => c.IdLocal != consulta.IdLocal)
            .Where(c => c.DataConsulta >= consulta.DataConsulta.Date && c.DataConsulta < consulta.DataConsulta.Date.AddDays(1))
            .Where(c => c.Horario == consulta.Horario)
            .Where(c => c.Status != "Cancelada")
            .Where(c =>
                string.IsNullOrWhiteSpace(consulta.ProfissionalSalaIdLocal) ||
                string.IsNullOrWhiteSpace(c.ProfissionalSalaIdLocal) ||
                c.ProfissionalSalaIdLocal == consulta.ProfissionalSalaIdLocal)
            .FirstOrDefaultAsync();

        if (conflito != null)
        {
            throw new InvalidOperationException(
                $"Horario ocupado em {consulta.DataConsulta:dd/MM/yyyy} as {consulta.Horario} por {conflito.ClienteNome}.");
        }

        consulta.AtualizadoEm = DateTime.Now;
        if (consulta.Id == 0)
            await context.Consultas.AddAsync(consulta);
        else
            context.Consultas.Update(consulta);

        await context.SaveChangesAsync();
    }

    public Task MarkDeletedAsync(Consulta consulta)
    {
        // Nao delega ao SaveAsync: alem da validacao, a checagem de conflito
        // de horario impediria excluir uma consulta em slot disputado.
        return ExclusaoLocalStore.MarcarExcluidoAsync(_database, consulta);
    }
}
