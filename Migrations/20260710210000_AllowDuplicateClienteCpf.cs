using AgendamentoWpfApp.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoWpfApp.Migrations
{
    [DbContext(typeof(AgendaDbContext))]
    [Migration("20260710210000_AllowDuplicateClienteCpf")]
    public partial class AllowDuplicateClienteCpf : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_CLIENTES_Cpf", table: "CLIENTES");
            migrationBuilder.CreateIndex(name: "IX_CLIENTES_Cpf", table: "CLIENTES", column: "Cpf");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_CLIENTES_Cpf", table: "CLIENTES");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IX_CLIENTES_Cpf ON CLIENTES (Cpf) WHERE Cpf <> ''");
        }
    }
}
