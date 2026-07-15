using System;
using System.Linq;
using System.Threading.Tasks;
using AgendamentoWpfApp.Models;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoWpfApp.Data.Stores;

// Exclusao logica sem passar pelo SaveAsync dos stores: excluir nao valida
// nem normaliza o cadastro (registro legado invalido pelas regras atuais
// tambem precisa poder ser excluido e propagar ao portal). O UPDATE toca so
// Excluido e AtualizadoEm, deixando o registro pendente para o sync.
internal static class ExclusaoLocalStore
{
    public static async Task MarcarExcluidoAsync<T>(AgendaDatabase database, T registro)
        where T : class, IRegistroSincronizavel
    {
        registro.Excluido = true;
        registro.AtualizadoEm = DateTime.Now;

        await using var context = database.CreateContext();
        await context.Set<T>()
            .Where(r => r.IdLocal == registro.IdLocal)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Excluido, true)
                .SetProperty(r => r.AtualizadoEm, registro.AtualizadoEm));
    }
}
