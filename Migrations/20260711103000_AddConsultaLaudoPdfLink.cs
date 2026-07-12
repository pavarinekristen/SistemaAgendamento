using System;
using AgendamentoWpfApp.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoWpfApp.Migrations
{
    [DbContext(typeof(AgendaDbContext))]
    [Migration("20260711103000_AddConsultaLaudoPdfLink")]
    public partial class AddConsultaLaudoPdfLink : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LaudoGeradoEm",
                table: "CONSULTAS",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LaudoPdfNomeArquivo",
                table: "CONSULTAS",
                type: "TEXT",
                maxLength: 260,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LaudoPdfPath",
                table: "CONSULTAS",
                type: "TEXT",
                maxLength: 600,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LaudoPdfUri",
                table: "CONSULTAS",
                type: "TEXT",
                maxLength: 800,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LaudoTipo",
                table: "CONSULTAS",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CONSULTAS_LaudoGeradoEm",
                table: "CONSULTAS",
                column: "LaudoGeradoEm");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nao removemos colunas em rollback para preservar links/metadados de laudos ja gerados.
        }
    }
}
