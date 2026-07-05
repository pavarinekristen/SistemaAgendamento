using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoWpfApp.Migrations
{
    public partial class AddConsultas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CONSULTAS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdLocal = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ClienteIdLocal = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ClienteNome = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    DataConsulta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Horario = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Local = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ProfissionalSala = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Observacoes = table.Column<string>(type: "TEXT", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SincronizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    HashSincronizacao = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONSULTAS", x => x.Id);
                });

            migrationBuilder.CreateIndex(name: "IX_CONSULTAS_ClienteIdLocal", table: "CONSULTAS", column: "ClienteIdLocal");
            migrationBuilder.CreateIndex(name: "IX_CONSULTAS_DataConsulta", table: "CONSULTAS", column: "DataConsulta");
            migrationBuilder.CreateIndex(name: "IX_CONSULTAS_DataConsulta_Horario", table: "CONSULTAS", columns: new[] { "DataConsulta", "Horario" });
            migrationBuilder.CreateIndex(name: "IX_CONSULTAS_IdLocal", table: "CONSULTAS", column: "IdLocal", unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CONSULTAS");
        }
    }
}
