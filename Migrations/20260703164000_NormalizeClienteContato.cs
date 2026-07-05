using AgendamentoWpfApp.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoWpfApp.Migrations
{
    [DbContext(typeof(AgendaDbContext))]
    [Migration("20260703164000_NormalizeClienteContato")]
    public partial class NormalizeClienteContato : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE CLIENTES
                   SET Cpf = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(Cpf, '.', ''), '-', ''), '/', ''), ' ', ''), '_', ''),
                       Telefone = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(Telefone, '(', ''), ')', ''), '-', ''), ' ', ''), '.', ''), '+', '')
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
