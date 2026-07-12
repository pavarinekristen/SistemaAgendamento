using AgendamentoWpfApp.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoWpfApp.Migrations
{
    [DbContext(typeof(AgendaDbContext))]
    [Migration("20260711100000_AddLaudosReportIndexes")]
    public partial class AddLaudosReportIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CONSULTAS_ClienteNome",
                table: "CONSULTAS",
                column: "ClienteNome");

            migrationBuilder.CreateIndex(
                name: "IX_CONSULTAS_Motivo_DataConsulta",
                table: "CONSULTAS",
                columns: new[] { "Motivo", "DataConsulta" });

            migrationBuilder.CreateIndex(
                name: "IX_CONSULTAS_ClienteNome_DataConsulta",
                table: "CONSULTAS",
                columns: new[] { "ClienteNome", "DataConsulta" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CONSULTAS_ClienteNome",
                table: "CONSULTAS");

            migrationBuilder.DropIndex(
                name: "IX_CONSULTAS_Motivo_DataConsulta",
                table: "CONSULTAS");

            migrationBuilder.DropIndex(
                name: "IX_CONSULTAS_ClienteNome_DataConsulta",
                table: "CONSULTAS");
        }
    }
}
