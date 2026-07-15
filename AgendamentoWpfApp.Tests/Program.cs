using AgendamentoWpfApp.Models;
using AgendamentoWpfApp.Services;
using AgendamentoWpfApp.Services.Validation;
using Microsoft.Data.Sqlite;
using PdfSharpCore.Pdf.IO;

var tempFolder = Path.Combine(Path.GetTempPath(), "SparkCoreUnitTests", Guid.NewGuid().ToString("N"));
var databasePath = Path.Combine(tempFolder, "agenda-tests.sqlite");

try
{
    RunValidatorTests();
    await RunWorkflowTestsAsync(databasePath);
    await RunIncrementalSnapshotTestsAsync(Path.Combine(tempFolder, "agenda-sync-tests.sqlite"));
    RunLaudoPdfTests();

    Console.WriteLine("Testes unitarios concluidos com sucesso.");
    return 0;
}
finally
{
    SqliteConnection.ClearAllPools();

    try
    {
        if (Directory.Exists(tempFolder))
            Directory.Delete(tempFolder, recursive: true);
    }
    catch (IOException)
    {
        Console.WriteLine($"Aviso: nao foi possivel remover pasta temporaria {tempFolder}.");
    }
}

static void RunLaudoPdfTests()
{
    var service = new LaudoPdfService();
    var cliente = new Cliente
    {
        Nome = "Cliente Laudo",
        Empresa = "Empresa Laudo",
        Cargo = "Vigilante",
        Cpf = "12345678909",
        Status = "Ativo"
    };

    var semArma = new Consulta
    {
        ClienteIdLocal = cliente.IdLocal,
        ClienteNome = cliente.Nome,
        Empresa = cliente.Empresa,
        ClienteCargo = cliente.Cargo,
        DataConsulta = DateTime.Today,
        Horario = "08:00",
        Local = "Empresa",
        Motivo = "Admissao",
        TrabalhaArmado = false
    };

    var comArma = new Consulta
    {
        ClienteIdLocal = cliente.IdLocal,
        ClienteNome = cliente.Nome,
        Empresa = cliente.Empresa,
        ClienteCargo = cliente.Cargo,
        DataConsulta = DateTime.Today,
        Horario = "09:00",
        Local = "Empresa",
        Motivo = "Admissao",
        TrabalhaArmado = true
    };

    var semArmaResult = service.GerarLaudo(cliente, semArma);
    var comArmaResult = service.GerarLaudo(cliente, comArma);

    Assert(File.Exists(semArmaResult.Path), "Laudo sem arma nao foi gerado.");
    Assert(File.Exists(comArmaResult.Path), "Laudo com arma nao foi gerado.");
    Assert(semArmaResult.FileName.Contains("SemArma"), "Nome do laudo sem arma nao indica o tipo.");
    Assert(comArmaResult.FileName.Contains("ComArma"), "Nome do laudo com arma nao indica o tipo.");

    using var semArmaPdf = PdfReader.Open(semArmaResult.Path, PdfDocumentOpenMode.ReadOnly);
    using var comArmaPdf = PdfReader.Open(comArmaResult.Path, PdfDocumentOpenMode.ReadOnly);
    Assert(semArmaPdf.PageCount == 1, "Laudo sem arma deve conter uma pagina.");
    Assert(comArmaPdf.PageCount == 1, "Laudo com arma deve conter uma pagina.");
}

static void RunValidatorTests()
{
    Assert(InputNormalizer.NormalizeCpf("123.456.789-09") == "12345678909", "CPF nao foi normalizado.");
    Assert(InputNormalizer.FormatCpf("12345678909") == "123.456.789-09", "CPF nao foi formatado.");
    Assert(InputNormalizer.NormalizeTelefone("(11) 99999-9999") == "11999999999", "Telefone nao foi normalizado.");
    Assert(InputNormalizer.IsValidCpf("12345678909"), "CPF valido foi rejeitado.");
    Assert(!InputNormalizer.IsValidCpf("11111111111"), "CPF repetido foi aceito.");

    var clienteInvalido = new Cliente
    {
        Nome = "",
        Cpf = "11111111111",
        Email = "email-invalido",
        Telefone = "123",
        DataNascimento = DateTime.Today.AddDays(1)
    };
    var clienteValidation = ClienteValidator.Validate(clienteInvalido);
    Assert(!clienteValidation.IsValid, "Cliente invalido foi aceito.");
    Assert(clienteValidation.Message.Contains("Informe o nome"), "Erro de nome nao retornado.");
    Assert(clienteValidation.Message.Contains("CPF invalido"), "Erro de CPF nao retornado.");

    var consultaInvalida = new Consulta
    {
        ClienteIdLocal = "",
        DataConsulta = DateTime.Today.AddDays(-1),
        Horario = "",
        Motivo = "",
        Local = ""
    };
    var consultaValidation = ConsultaValidator.Validate(consultaInvalida);
    Assert(!consultaValidation.IsValid, "Consulta invalida foi aceita.");
    Assert(consultaValidation.Message.Contains("Selecione um cliente"), "Erro de cliente da consulta nao retornado.");
    Assert(consultaValidation.Message.Contains("Selecione o motivo"), "Erro de motivo da consulta nao retornado.");

    var profissionalInvalido = new ProfissionalSala
    {
        Nome = "",
        Tipo = "",
        Email = "email-invalido"
    };
    var profissionalValidation = ProfissionalSalaValidator.Validate(profissionalInvalido);
    Assert(!profissionalValidation.IsValid, "Profissional invalido foi aceito.");
    Assert(profissionalValidation.Message.Contains("Informe o nome"), "Erro de nome do profissional nao retornado.");
}

static async Task RunWorkflowTestsAsync(string databasePath)
{
    using var workspace = new AgendaWorkspaceService(databasePath);
    var clienteWorkflow = new ClienteWorkflowService(workspace);
    var profissionalWorkflow = new ProfissionalSalaWorkflowService(workspace);
    var consultaWorkflow = new ConsultaWorkflowService(workspace);

    await workspace.MigrateAsync();

    var cliente = new Cliente
    {
        Nome = "Cliente Workflow",
        Empresa = "Empresa Teste",
        Cargo = "Vigilante",
        Cpf = "123.456.789-09",
        Email = "cliente.workflow@example.com",
        Telefone = "(11) 99999-9999",
        Status = "Ativo"
    };
    await clienteWorkflow.SaveAsync(cliente);

    var clientes = await clienteWorkflow.LoadAsync();
    Assert(clientes.Count == 1, "Cliente nao foi salvo pelo workflow.");
    Assert((await clienteWorkflow.SearchAsync("workflow")).Count == 1, "Busca por nome falhou.");
    Assert((await clienteWorkflow.SearchAsync("12345678909")).Count == 1, "Busca por CPF falhou.");

    var duplicado = new Cliente
    {
        Nome = "Cliente Duplicado",
        Empresa = "Empresa Teste",
        Cargo = "Vigilante",
        Cpf = "12345678909",
        Status = "Ativo"
    };
    await clienteWorkflow.SaveAsync(duplicado);

    clientes = await clienteWorkflow.LoadAsync();
    Assert(clientes.Count == 2, "CPF duplicado deve ser permitido para dados legados.");

    // Novo cadastro deve receber ID sequencial do banco automaticamente.
    Assert(cliente.Id > 0, "Novo cliente nao recebeu ID apos salvar.");
    Assert(duplicado.Id == cliente.Id + 1, "IDs de novos clientes nao seguem a sequencia.");

    // Busca por ID direto no banco (com e sem zeros a esquerda). Termo numerico
    // tambem casa com CPF/RG/telefone por LIKE, entao valida presenca, nao contagem.
    var porIdSimples = await clienteWorkflow.SearchAsync(cliente.Id.ToString());
    Assert(porIdSimples.Any(c => c.IdLocal == cliente.IdLocal), "Busca por ID falhou.");

    var porId = await clienteWorkflow.SearchAsync(duplicado.Id.ToString("0000"));
    Assert(porId.Count == 1 && porId[0].IdLocal == duplicado.IdLocal, "Busca por ID no banco falhou.");

    // Pesquisa paginada no banco (grade principal nao carrega mais tudo em memoria).
    Assert(await clienteWorkflow.CountAsync() == 2, "Contagem geral de clientes falhou.");
    Assert(await clienteWorkflow.CountAsync("duplicado") == 1, "Contagem filtrada por termo falhou.");
    Assert(await clienteWorkflow.CountAsync("", "Empresa Teste") == 2, "Contagem filtrada por empresa falhou.");
    // O filtro de empresa e igualdade exata: o valor vem do dropdown (do banco).
    Assert(await clienteWorkflow.CountAsync("", "empresa teste") == 0, "Filtro de empresa deveria ser igualdade exata.");
    Assert(await clienteWorkflow.CountAsync("duplicado", "Outra Empresa") == 0, "Filtro por empresa nao restringiu a contagem.");

    var pagina1 = await clienteWorkflow.SearchPageAsync("", "", skip: 0, take: 10);
    Assert(pagina1.Count == 2, "Pagina unica deveria trazer os dois clientes.");
    Assert(pagina1[0].Nome == "Cliente Duplicado", "Paginacao nao ordenou por nome.");

    var pagina2 = await clienteWorkflow.SearchPageAsync("", "", skip: 1, take: 10);
    Assert(pagina2.Count == 1 && pagina2[0].IdLocal == cliente.IdLocal, "Skip da paginacao falhou.");

    var empresas = await clienteWorkflow.ListEmpresasAsync();
    Assert(empresas.Count == 1 && empresas[0] == "Empresa Teste", "Lista de empresas distintas falhou.");

    // Busca normalizada: caixa e acentos nao importam; curingas sao literais.
    var acentuado = new Cliente
    {
        Nome = "José Ângelo",
        Empresa = "Empresa Teste",
        Cargo = "Vigilante",
        Status = "Ativo"
    };
    await clienteWorkflow.SaveAsync(acentuado);

    Assert((await clienteWorkflow.SearchAsync("jose angelo")).Count == 1, "Busca sem acento nao encontrou nome acentuado.");
    Assert((await clienteWorkflow.SearchAsync("JOSÉ")).Count == 1, "Busca acentuada em caixa alta falhou.");
    Assert((await clienteWorkflow.SearchAsync("100%")).Count == 0, "Curinga % deveria ser tratado como texto literal.");
    Assert((await clienteWorkflow.SearchAsync("C_IENTE")).Count == 0, "Curinga _ deveria ser tratado como texto literal.");

    await clienteWorkflow.DeleteAsync(acentuado);
    Assert(await clienteWorkflow.CountAsync() == 2, "Exclusao do cliente acentuado falhou.");

    var sala1 = new ProfissionalSala
    {
        Nome = "Sala Workflow 1",
        Tipo = "Sala",
        EspecialidadeFuncao = "Atendimento",
        Ativo = true
    };
    var sala2 = new ProfissionalSala
    {
        Nome = "Sala Workflow 2",
        Tipo = "Sala",
        EspecialidadeFuncao = "Atendimento",
        Ativo = true
    };
    await profissionalWorkflow.SaveAsync(sala1);
    await profissionalWorkflow.SaveAsync(sala2);

    var profissionais = await profissionalWorkflow.LoadAsync();
    Assert(profissionais.Count == 2, "Profissionais nao foram salvos pelo workflow.");

    var salvo = clientes[0];
    var dataConsulta = DateTime.Today.AddDays(3);
    var consulta = new Consulta
    {
        ClienteIdLocal = salvo.IdLocal,
        ClienteNome = salvo.Nome,
        DataConsulta = dataConsulta,
        Horario = "14:00",
        Local = "Sala Workflow 1",
        ProfissionalSala = sala1.Nome,
        ProfissionalSalaIdLocal = sala1.IdLocal,
        Status = "Agendada"
    };
    await consultaWorkflow.SaveAsync(consulta);

    var conflito = new Consulta
    {
        ClienteIdLocal = salvo.IdLocal,
        ClienteNome = "Outro Cliente",
        DataConsulta = dataConsulta,
        Horario = "14:00",
        Local = "Sala Workflow 1",
        ProfissionalSala = sala1.Nome,
        ProfissionalSalaIdLocal = sala1.IdLocal,
        Status = "Agendada"
    };
    await AssertThrowsAsync<InvalidOperationException>(
        () => consultaWorkflow.SaveAsync(conflito),
        "Conflito de horario nao foi bloqueado pelo workflow.");

    var mesmaHoraOutraSala = new Consulta
    {
        ClienteIdLocal = salvo.IdLocal,
        ClienteNome = "Outro Cliente",
        DataConsulta = dataConsulta,
        Horario = "14:00",
        Local = "Sala Workflow 2",
        ProfissionalSala = sala2.Nome,
        ProfissionalSalaIdLocal = sala2.IdLocal,
        Status = "Agendada"
    };
    await consultaWorkflow.SaveAsync(mesmaHoraOutraSala);

    var consultas = await consultaWorkflow.LoadByDateAsync(dataConsulta);
    Assert(consultas.Count == 2, "Mesma hora em sala diferente deveria ser permitida.");

    await consultaWorkflow.DeleteAsync(consultas[0]);
    consultas = await consultaWorkflow.LoadByDateAsync(dataConsulta);
    Assert(consultas.Count == 1, "Delete de consulta pelo workflow falhou.");

    await clienteWorkflow.DeleteAsync(salvo);
    clientes = await clienteWorkflow.LoadAsync();
    Assert(clientes.Count == 1, "Delete de cliente pelo workflow falhou.");
}

static async Task RunIncrementalSnapshotTestsAsync(string databasePath)
{
    using var workspace = new AgendaWorkspaceService(databasePath);
    var clienteWorkflow = new ClienteWorkflowService(workspace);
    var profissionalWorkflow = new ProfissionalSalaWorkflowService(workspace);
    await workspace.MigrateAsync();

    var cliente = new Cliente
    {
        Nome = "Cliente Sync",
        Empresa = "Empresa Sync",
        Cargo = "Vigilante",
        Cpf = "123.456.789-09",
        Status = "Ativo"
    };
    await clienteWorkflow.SaveAsync(cliente);

    var syncService = new AgendaSnapshotSyncService(databasePath);

    var lotes = await ColetarLotesAsync(syncService, completo: false);
    Assert(lotes.All(l => !l.SnapshotCompleto), "Nenhum lote pode afirmar snapshot completo.");
    Assert(RegistrosDaTabela(lotes, "CLIENTES") == 1, "Cliente pendente nao entrou no snapshot incremental.");

    // O blob local de pesquisa e derivavel e duplicaria dado pessoal no
    // Postgres; a coluna existe no SQLite mas nunca viaja no snapshot.
    var dadosClientes = lotes
        .SelectMany(l => l.Tabelas)
        .Where(t => string.Equals(t.Nome, "CLIENTES", StringComparison.OrdinalIgnoreCase))
        .SelectMany(t => t.Registros)
        .Select(r => r.DadosJson)
        .ToList();
    Assert(dadosClientes.Count == 1, "Payload do cliente pendente nao foi localizado.");
    Assert(dadosClientes.All(d => !d.Contains("PesquisaNormalizada", StringComparison.OrdinalIgnoreCase)),
        "PesquisaNormalizada nao pode viajar no payload do snapshot.");
    Assert(dadosClientes.All(d => d.Contains("Cliente Sync", StringComparison.Ordinal)),
        "Filtro de colunas locais removeu dados legitimos do payload.");

    foreach (var lote in lotes)
        await syncService.MarcarRegistrosComoSincronizadosAsync(lote, DateTime.Now);

    lotes = await ColetarLotesAsync(syncService, completo: false);
    Assert(RegistrosDaTabela(lotes, "CLIENTES") == 0, "Cliente ja sincronizado foi reenviado no snapshot incremental.");
    // Sem pendencias ainda sai um unico request vazio (valida sessao e registra
    // o dispositivo), nunca marcado como completo: um "completo" vazio
    // autorizaria o servidor a excluir tudo do dispositivo.
    Assert(lotes.Count == 1 && lotes[0].Tabelas.Count == 0, "Sem pendencias deveria gerar um unico request vazio.");
    Assert(!lotes[0].SnapshotCompleto, "Request vazio nao pode ser marcado como snapshot completo.");

    lotes = await ColetarLotesAsync(syncService, completo: true);
    Assert(RegistrosDaTabela(lotes, "CLIENTES") == 1, "Snapshot completo deve enviar todos os registros.");
    Assert(lotes.All(l => !l.SnapshotCompleto), "Mesmo o snapshot completo viaja em lotes parciais.");

    // Garante que o relogio avancou: AtualizadoEm precisa ficar maior que SincronizadoEm.
    await Task.Delay(50);
    cliente.Cargo = "Vigilante Lider";
    await clienteWorkflow.SaveAsync(cliente);

    lotes = await ColetarLotesAsync(syncService, completo: false);
    Assert(RegistrosDaTabela(lotes, "CLIENTES") == 1, "Cliente editado nao voltou a ficar pendente no snapshot incremental.");

    // Loteamento em streaming: 5 clientes + 2 salas com lotes de ate 2 registros.
    for (var i = 1; i <= 4; i++)
        await clienteWorkflow.SaveAsync(new Cliente
        {
            Nome = $"Cliente Lote {i}",
            Empresa = "Empresa Sync",
            Cargo = "Vigilante",
            Status = "Ativo"
        });
    await profissionalWorkflow.SaveAsync(new ProfissionalSala { Nome = "Sala Lote 1", Tipo = "Sala", EspecialidadeFuncao = "Atendimento", Ativo = true });
    await profissionalWorkflow.SaveAsync(new ProfissionalSala { Nome = "Sala Lote 2", Tipo = "Sala", EspecialidadeFuncao = "Atendimento", Ativo = true });

    lotes = await ColetarLotesAsync(syncService, completo: true, maxRegistros: 2);
    var totalRegistros = lotes.Sum(l => l.Tabelas.Sum(t => t.Registros.Count));
    Assert(totalRegistros == 7, $"Loteamento perdeu ou duplicou registros (esperados 7, obtidos {totalRegistros}).");
    Assert(lotes.Count == 4, $"Esperados 4 lotes de ate 2 registros, obtidos {lotes.Count}.");
    Assert(lotes.All(l => l.Tabelas.Sum(t => t.Registros.Count) <= 2), "Lote excedeu o tamanho maximo.");
    Assert(lotes.All(l => !l.SnapshotCompleto), "Lote parcial nao pode ser marcado como snapshot completo.");
    Assert(lotes.All(l => l.DispositivoId == Environment.MachineName), "Lote perdeu o DispositivoId.");
    Assert(RegistrosDaTabela(lotes, "CLIENTES") == 5, "Total de clientes no loteamento errado.");
    Assert(RegistrosDaTabela(lotes, "PROFISSIONAIS_SALAS") == 2, "Total de salas no loteamento errado.");
}

static async Task<List<AgendaSnapshotRequest>> ColetarLotesAsync(
    AgendaSnapshotSyncService service, bool completo, int maxRegistros = 1000)
{
    var lotes = new List<AgendaSnapshotRequest>();
    await foreach (var lote in service.CriarLotesAsync(completo, maxRegistros))
        lotes.Add(lote);
    return lotes;
}

static int RegistrosDaTabela(List<AgendaSnapshotRequest> lotes, string tabela)
{
    return lotes.Sum(l => l.Tabelas
        .Where(t => string.Equals(t.Nome, tabela, StringComparison.OrdinalIgnoreCase))
        .Sum(t => t.Registros.Count));
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static async Task AssertThrowsAsync<TException>(Func<Task> action, string message)
    where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}
