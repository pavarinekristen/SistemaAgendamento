using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoWpfApp.Migrations
{
    public partial class InitialAgendaSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CLIENTES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdLocal = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Cpf = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    DataNascimento = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Endereco = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                    Bairro = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Cidade = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    LocalAtendimento = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    DataAgendamento = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HorarioAgendamento = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
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
                    table.PrimaryKey("PK_CLIENTES", x => x.Id);
                });

            migrationBuilder.CreateIndex(name: "IX_CLIENTES_Cpf", table: "CLIENTES", column: "Cpf");
            migrationBuilder.CreateIndex(name: "IX_CLIENTES_IdLocal", table: "CLIENTES", column: "IdLocal", unique: true);
            migrationBuilder.CreateIndex(name: "IX_CLIENTES_Nome", table: "CLIENTES", column: "Nome");
            migrationBuilder.CreateIndex(name: "IX_CLIENTES_Telefone", table: "CLIENTES", column: "Telefone");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CLIENTES");
        }
    }
}
