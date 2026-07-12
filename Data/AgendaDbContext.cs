using System;
using System.IO;
using AgendamentoWpfApp.Models;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoWpfApp.Data;

internal sealed class AgendaDbContext : DbContext
{
    private readonly string? _databasePath;

    public AgendaDbContext()
    {
    }

    public AgendaDbContext(string databasePath)
    {
        _databasePath = databasePath;
    }

    public AgendaDbContext(DbContextOptions<AgendaDbContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Consulta> Consultas => Set<Consulta>();
    public DbSet<ProfissionalSala> ProfissionaisSalas => Set<ProfissionalSala>();

    public static string DefaultDatabasePath
    {
        get
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RetaguardaAgendamento");

            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "agenda.sqlite");
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlite($"Data Source={_databasePath ?? DefaultDatabasePath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasIndex(e => e.IdLocal).IsUnique();
            entity.HasIndex(e => e.Nome);
            entity.HasIndex(e => e.Empresa);
            entity.HasIndex(e => e.Cargo);
            entity.HasIndex(e => e.Cpf);
            entity.HasIndex(e => e.Telefone);
            entity.Property(e => e.Excluido).HasDefaultValue(false);
        });

        modelBuilder.Entity<Consulta>(entity =>
        {
            entity.HasIndex(e => e.IdLocal).IsUnique();
            entity.HasIndex(e => e.ClienteIdLocal);
            entity.HasIndex(e => e.ClienteNome);
            entity.HasIndex(e => e.Empresa);
            entity.HasIndex(e => e.DataConsulta);
            entity.HasIndex(e => e.LaudoGeradoEm);
            entity.HasIndex(e => e.Motivo);
            entity.HasIndex(e => e.ProfissionalSalaIdLocal);
            entity.HasIndex(e => new { e.DataConsulta, e.Horario });
            entity.HasIndex(e => new { e.Empresa, e.DataConsulta });
            entity.HasIndex(e => new { e.Motivo, e.DataConsulta });
            entity.HasIndex(e => new { e.ClienteNome, e.DataConsulta });
            entity.Property(e => e.Excluido).HasDefaultValue(false);
        });

        modelBuilder.Entity<ProfissionalSala>(entity =>
        {
            entity.HasIndex(e => e.IdLocal).IsUnique();
            entity.HasIndex(e => e.Nome);
            entity.HasIndex(e => e.Tipo);
            entity.Property(e => e.Ativo).HasDefaultValue(true);
            entity.Property(e => e.Excluido).HasDefaultValue(false);
        });
    }
}
