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
    Assert(await clienteWorkflow.CountAsync("", "empresa teste") == 2, "Contagem filtrada por empresa falhou.");
    Assert(await clienteWorkflow.CountAsync("duplicado", "Outra Empresa") == 0, "Filtro por empresa nao restringiu a contagem.");

    var pagina1 = await clienteWorkflow.SearchPageAsync("", "", skip: 0, take: 10);
    Assert(pagina1.Count == 2, "Pagina unica deveria trazer os dois clientes.");
    Assert(pagina1[0].Nome == "Cliente Duplicado", "Paginacao nao ordenou por nome.");

    var pagina2 = await clienteWorkflow.SearchPageAsync("", "", skip: 1, take: 10);
    Assert(pagina2.Count == 1 && pagina2[0].IdLocal == cliente.IdLocal, "Skip da paginacao falhou.");

    var empresas = await clienteWorkflow.ListEmpresasAsync();
    Assert(empresas.Count == 1 && empresas[0] == "Empresa Teste", "Lista de empresas distintas falhou.");

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

    var snapshot = await syncService.CriarSnapshotAsync(completo: false);
    Assert(!snapshot.SnapshotCompleto, "Snapshot incremental marcado como completo.");
    Assert(RegistrosDaTabela(snapshot, "CLIENTES") == 1, "Cliente pendente nao entrou no snapshot incremental.");

    await syncService.MarcarRegistrosComoSincronizadosAsync(snapshot, DateTime.Now);

    snapshot = await syncService.CriarSnapshotAsync(completo: false);
    Assert(RegistrosDaTabela(snapshot, "CLIENTES") == 0, "Cliente ja sincronizado foi reenviado no snapshot incremental.");

    snapshot = await syncService.CriarSnapshotAsync(completo: true);
    Assert(snapshot.SnapshotCompleto, "Snapshot completo nao marcado como completo.");
    Assert(RegistrosDaTabela(snapshot, "CLIENTES") == 1, "Snapshot completo deve enviar todos os registros.");

    // Garante que o relogio avancou: AtualizadoEm precisa ficar maior que SincronizadoEm.
    await Task.Delay(50);
    cliente.Cargo = "Vigilante Lider";
    await clienteWorkflow.SaveAsync(cliente);

    snapshot = await syncService.CriarSnapshotAsync(completo: false);
    Assert(RegistrosDaTabela(snapshot, "CLIENTES") == 1, "Cliente editado nao voltou a ficar pendente no snapshot incremental.");

    RunLoteamentoTests();
}

static void RunLoteamentoTests()
{
    var snapshot = new AgendaSnapshotRequest { DispositivoId = "TESTE" };
    var clientes = new AgendaSnapshotTable { Nome = "CLIENTES" };
    for (var i = 0; i < 5; i++)
        clientes.Registros.Add(new AgendaSnapshotRecord { IdLocal = $"c{i}", DadosJson = "{}", Hash = $"h{i}" });

    var consultas = new AgendaSnapshotTable { Nome = "CONSULTAS" };
    for (var i = 0; i < 3; i++)
        consultas.Registros.Add(new AgendaSnapshotRecord { IdLocal = $"a{i}", DadosJson = "{}", Hash = $"h{i}" });

    snapshot.Tabelas.Add(clientes);
    snapshot.Tabelas.Add(consultas);

    var lotes = AgendaSnapshotSyncService.DividirEmLotes(snapshot, maxRegistros: 2);
    Assert(lotes.Count == 4, $"Esperados 4 lotes de ate 2 registros, obtidos {lotes.Count}.");

    var totalRegistros = lotes.Sum(l => l.Tabelas.Sum(t => t.Registros.Count));
    Assert(totalRegistros == 8, "Loteamento perdeu ou duplicou registros.");
    Assert(lotes.All(l => l.Tabelas.Sum(t => t.Registros.Count) <= 2), "Lote excedeu o tamanho maximo.");
    Assert(lotes.All(l => !l.SnapshotCompleto), "Lote parcial nao pode ser marcado como snapshot completo.");
    Assert(lotes.All(l => l.DispositivoId == "TESTE"), "Lote perdeu o DispositivoId.");

    // Snapshot vazio ainda gera um unico request (valida sessao e registra o dispositivo).
    var vazio = AgendaSnapshotSyncService.DividirEmLotes(new AgendaSnapshotRequest { DispositivoId = "TESTE" }, maxRegistros: 2);
    Assert(vazio.Count == 1 && vazio[0].Tabelas.Count == 0, "Snapshot vazio deveria gerar um unico lote vazio.");
}

static int RegistrosDaTabela(AgendaSnapshotRequest snapshot, string tabela)
{
    var encontrada = snapshot.Tabelas.FirstOrDefault(t => string.Equals(t.Nome, tabela, StringComparison.OrdinalIgnoreCase));
    return encontrada?.Registros.Count ?? 0;
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
