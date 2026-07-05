using System;
using AgendamentoWpfApp.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoWpfApp.Migrations
{
    [DbContext(typeof(AgendaDbContext))]
    [Migration("20260703150000_AddProfissionaisSalas")]
    public partial class AddProfissionaisSalas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfissionalSalaIdLocal",
                table: "CONSULTAS",
                type: "TEXT",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PROFISSIONAIS_SALAS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdLocal = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 140, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    EspecialidadeFuncao = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    Observacoes = table.Column<string>(type: "TEXT", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SincronizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    HashSincronizacao = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROFISSIONAIS_SALAS", x => x.Id);
                });

            migrationBuilder.CreateIndex(name: "IX_CONSULTAS_ProfissionalSalaIdLocal", table: "CONSULTAS", column: "ProfissionalSalaIdLocal");
            migrationBuilder.CreateIndex(name: "IX_PROFISSIONAIS_SALAS_IdLocal", table: "PROFISSIONAIS_SALAS", column: "IdLocal", unique: true);
            migrationBuilder.CreateIndex(name: "IX_PROFISSIONAIS_SALAS_Nome", table: "PROFISSIONAIS_SALAS", column: "Nome");
            migrationBuilder.CreateIndex(name: "IX_PROFISSIONAIS_SALAS_Tipo", table: "PROFISSIONAIS_SALAS", column: "Tipo");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PROFISSIONAIS_SALAS");
            migrationBuilder.DropIndex(name: "IX_CONSULTAS_ProfissionalSalaIdLocal", table: "CONSULTAS");
            migrationBuilder.DropColumn(name: "ProfissionalSalaIdLocal", table: "CONSULTAS");
        }
    }
}
