using System;
using AgendamentoWpfApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoWpfApp.Migrations
{
    [DbContext(typeof(AgendaDbContext))]
    [Migration("20260703120000_InitialAgendaSchema")]
    partial class InitialAgendaSchema
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("AgendamentoWpfApp.Models.Cliente", b =>
                {
                    b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                    b.Property<DateTime>("AtualizadoEm").HasColumnType("TEXT");
                    b.Property<string>("Bairro").IsRequired().HasMaxLength(120).HasColumnType("TEXT");
                    b.Property<string>("Cidade").IsRequired().HasMaxLength(120).HasColumnType("TEXT");
                    b.Property<string>("Cpf").IsRequired().HasMaxLength(14).HasColumnType("TEXT");
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
                    b.Property<string>("Telefone").IsRequired().HasMaxLength(30).HasColumnType("TEXT");

                    b.HasKey("Id");
                    b.HasIndex("Cpf");
                    b.HasIndex("IdLocal").IsUnique();
                    b.HasIndex("Nome");
                    b.HasIndex("Telefone");
                    b.ToTable("CLIENTES");
                });
#pragma warning restore 612, 618
        }
    }
}
