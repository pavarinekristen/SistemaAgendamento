using AgendamentoWpfApp.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoWpfApp.Migrations
{
    [DbContext(typeof(AgendaDbContext))]
    [Migration("20260707100000_AddClienteLaudoFields")]
    public partial class AddClienteLaudoFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Empresa",
                table: "CLIENTES",
                type: "TEXT",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Escolaridade",
                table: "CLIENTES",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Cargo",
                table: "CLIENTES",
                type: "TEXT",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EstadoCivil",
                table: "CLIENTES",
                type: "TEXT",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Naturalidade",
                table: "CLIENTES",
                type: "TEXT",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Rg",
                table: "CLIENTES",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sexo",
                table: "CLIENTES",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoEndereco",
                table: "CLIENTES",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Particular");

            migrationBuilder.AddColumn<string>(
                name: "Empresa",
                table: "CONSULTAS",
                type: "TEXT",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClienteCargo",
                table: "CONSULTAS",
                type: "TEXT",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Motivo",
                table: "CONSULTAS",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: "Admissao");

            migrationBuilder.AddColumn<bool>(
                name: "TrabalhaArmado",
                table: "CONSULTAS",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_CLIENTES_Cargo",
                table: "CLIENTES",
                column: "Cargo");

            migrationBuilder.CreateIndex(
                name: "IX_CLIENTES_Empresa",
                table: "CLIENTES",
                column: "Empresa");

            migrationBuilder.CreateIndex(
                name: "IX_CONSULTAS_Empresa",
                table: "CONSULTAS",
                column: "Empresa");

            migrationBuilder.CreateIndex(
                name: "IX_CONSULTAS_Motivo",
                table: "CONSULTAS",
                column: "Motivo");

            migrationBuilder.CreateIndex(
                name: "IX_CONSULTAS_Empresa_DataConsulta",
                table: "CONSULTAS",
                columns: new[] { "Empresa", "DataConsulta" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_CLIENTES_Cargo", table: "CLIENTES");
            migrationBuilder.DropIndex(name: "IX_CLIENTES_Empresa", table: "CLIENTES");
            migrationBuilder.DropIndex(name: "IX_CONSULTAS_Empresa", table: "CONSULTAS");
            migrationBuilder.DropIndex(name: "IX_CONSULTAS_Motivo", table: "CONSULTAS");
            migrationBuilder.DropIndex(name: "IX_CONSULTAS_Empresa_DataConsulta", table: "CONSULTAS");

            migrationBuilder.DropColumn(name: "Empresa", table: "CLIENTES");
            migrationBuilder.DropColumn(name: "Escolaridade", table: "CLIENTES");
            migrationBuilder.DropColumn(name: "Cargo", table: "CLIENTES");
            migrationBuilder.DropColumn(name: "EstadoCivil", table: "CLIENTES");
            migrationBuilder.DropColumn(name: "Naturalidade", table: "CLIENTES");
            migrationBuilder.DropColumn(name: "Rg", table: "CLIENTES");
            migrationBuilder.DropColumn(name: "Sexo", table: "CLIENTES");
            migrationBuilder.DropColumn(name: "TipoEndereco", table: "CLIENTES");
            migrationBuilder.DropColumn(name: "Empresa", table: "CONSULTAS");
            migrationBuilder.DropColumn(name: "ClienteCargo", table: "CONSULTAS");
            migrationBuilder.DropColumn(name: "Motivo", table: "CONSULTAS");
            migrationBuilder.DropColumn(name: "TrabalhaArmado", table: "CONSULTAS");
        }
    }
}
