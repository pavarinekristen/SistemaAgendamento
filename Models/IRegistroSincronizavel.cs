using System;

namespace AgendamentoWpfApp.Models;

// Contrato minimo que o sync incremental usa para marcar registros como
// sincronizados; toda tabela nova sincronizada deve implementa-lo e ganhar
// um case no dispatch de AgendaSnapshotSyncService.MarcarRegistrosComoSincronizadosAsync.
internal interface IRegistroSincronizavel
{
    string IdLocal { get; set; }
    DateTime AtualizadoEm { get; set; }
    DateTime? SincronizadoEm { get; set; }
}
