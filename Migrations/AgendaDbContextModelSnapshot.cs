using System;
using AgendamentoWpfApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace AgendamentoWpfApp.Migrations
{
    [DbContext(typeof(AgendaDbContext))]
    partial class AgendaDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("AgendamentoWpfApp.Models.Cliente", b =>
                {
                    b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                    b.Property<DateTime>("AtualizadoEm").HasColumnType("TEXT");
                    b.Property<string>("Bairro").IsRequired().HasMaxLength(120).HasColumnType("TEXT");
                    b.Property<string>("Cidade").IsRequired().HasMaxLength(120).HasColumnType("TEXT");
                    b.Property<string>("Cpf").IsRequired().HasMaxLength(11).HasColumnType("TEXT");
                    b.Property<DateTime>("CriadoEm").HasColumnType("TEXT");
                    b.Property<DateTime?>("DataAgendamento").HasColumnType("TEXT");
                    b.Property<DateTime?>("DataNascimento").HasColumnType("TEXT");
                    b.Property<string>("Email").IsRequired().HasMaxLength(180).HasColumnType("TEXT");
                    b.Property<string>("Endereco").IsRequired().HasMaxLength(240).HasColumnType("TEXT");
                    b.Property<bool>("Excluido").ValueGeneratedOnAdd().HasColumnType("INTEGER").HasDefaultValue(false);
                    b.Property<string>("HashSincronizacao").IsRequired().HasMaxLength(64).HasColumnType("TEXT");
                    b.Property<string>("HorarioAgendamento").IsRequired().HasMaxLength(10).HasColumnType("TEXT");
                    b.Property<string>("IdLocal").IsRequired().HasMaxLength(36).HasColumnType("TEXT");
                    b.Property<string>("LocalAtendimento").IsRequired().HasMaxLength(120).HasColumnType("TEXT");
                    b.Property<string>("Nome").IsRequired().HasMaxLength(160).HasColumnType("TEXT");
                    b.Property<string>("Observacoes").IsRequired().HasColumnType("TEXT");
                    b.Property<DateTime?>("SincronizadoEm").HasColumnType("TEXT");
                    b.Property<string>("Status").IsRequired().HasMaxLength(30).HasColumnType("TEXT");
                    b.Property<string>("Telefone").IsRequired().HasMaxLength(11).HasColumnType("TEXT");

                    b.HasKey("Id");
                    b.HasIndex("Cpf").IsUnique().HasFilter("Cpf <> ''");
                    b.HasIndex("IdLocal").IsUnique();
                    b.HasIndex("Nome");
                    b.HasIndex("Telefone");
                    b.ToTable("CLIENTES");
                });

            modelBuilder.Entity("AgendamentoWpfApp.Models.Consulta", b =>
                {
                    b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                    b.Property<DateTime>("AtualizadoEm").HasColumnType("TEXT");
                    b.Property<string>("ClienteIdLocal").IsRequired().HasMaxLength(36).HasColumnType("TEXT");
                    b.Property<string>("ClienteNome").IsRequired().HasMaxLength(160).HasColumnType("TEXT");
                    b.Property<DateTime>("CriadoEm").HasColumnType("TEXT");
                    b.Property<DateTime>("DataConsulta").HasColumnType("TEXT");
                    b.Property<bool>("Excluido").ValueGeneratedOnAdd().HasColumnType("INTEGER").HasDefaultValue(false);
                    b.Property<string>("HashSincronizacao").IsRequired().HasMaxLength(64).HasColumnType("TEXT");
                    b.Property<string>("Horario").IsRequired().HasMaxLength(10).HasColumnType("TEXT");
                    b.Property<string>("IdLocal").IsRequired().HasMaxLength(36).HasColumnType("TEXT");
                    b.Property<string>("Local").IsRequired().HasMaxLength(120).HasColumnType("TEXT");
                    b.Property<string>("Observacoes").IsRequired().HasColumnType("TEXT");
                    b.Property<string>("ProfissionalSalaIdLocal").IsRequired().HasMaxLength(36).HasColumnType("TEXT");
                    b.Property<string>("ProfissionalSala").IsRequired().HasMaxLength(120).HasColumnType("TEXT");
                    b.Property<DateTime?>("SincronizadoEm").HasColumnType("TEXT");
                    b.Property<string>("Status").IsRequired().HasMaxLength(30).HasColumnType("TEXT");

                    b.HasKey("Id");
                    b.HasIndex("ClienteIdLocal");
                    b.HasIndex("DataConsulta");
                    b.HasIndex("ProfissionalSalaIdLocal");
                    b.HasIndex("DataConsulta", "Horario");
                    b.HasIndex("IdLocal").IsUnique();
                    b.ToTable("CONSULTAS");
                });

            modelBuilder.Entity("AgendamentoWpfApp.Models.ProfissionalSala", b =>
                {
                    b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                    b.Property<bool>("Ativo").ValueGeneratedOnAdd().HasColumnType("INTEGER").HasDefaultValue(true);
                    b.Property<DateTime>("AtualizadoEm").HasColumnType("TEXT");
                    b.Property<DateTime>("CriadoEm").HasColumnType("TEXT");
                    b.Property<string>("Email").IsRequired().HasMaxLength(180).HasColumnType("TEXT");
                    b.Property<string>("EspecialidadeFuncao").IsRequired().HasMaxLength(120).HasColumnType("TEXT");
                    b.Property<bool>("Excluido").ValueGeneratedOnAdd().HasColumnType("INTEGER").HasDefaultValue(false);
                    b.Property<string>("HashSincronizacao").IsRequired().HasMaxLength(64).HasColumnType("TEXT");
                    b.Property<string>("IdLocal").IsRequired().HasMaxLength(36).HasColumnType("TEXT");
                    b.Property<string>("Nome").IsRequired().HasMaxLength(140).HasColumnType("TEXT");
                    b.Property<string>("Observacoes").IsRequired().HasColumnType("TEXT");
                    b.Property<DateTime?>("SincronizadoEm").HasColumnType("TEXT");
                    b.Property<string>("Telefone").IsRequired().HasMaxLength(30).HasColumnType("TEXT");
                    b.Property<string>("Tipo").IsRequired().HasMaxLength(30).HasColumnType("TEXT");

                    b.HasKey("Id");
                    b.HasIndex("IdLocal").IsUnique();
                    b.HasIndex("Nome");
                    b.HasIndex("Tipo");
                    b.ToTable("PROFISSIONAIS_SALAS");
                });
#pragma warning restore 612, 618
        }
    }
}
