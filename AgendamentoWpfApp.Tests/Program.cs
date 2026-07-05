using AgendamentoWpfApp.Models;
using AgendamentoWpfApp.Services;
using AgendamentoWpfApp.Services.Validation;
using Microsoft.Data.Sqlite;

var tempFolder = Path.Combine(Path.GetTempPath(), "SparkCoreUnitTests", Guid.NewGuid().ToString("N"));
var databasePath = Path.Combine(tempFolder, "agenda-tests.sqlite");

try
{
    RunValidatorTests();
    await RunWorkflowTestsAsync(databasePath);

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
        Local = ""
    };
    var consultaValidation = ConsultaValidator.Validate(consultaInvalida);
    Assert(!consultaValidation.IsValid, "Consulta invalida foi aceita.");
    Assert(consultaValidation.Message.Contains("Selecione um cliente"), "Erro de cliente da consulta nao retornado.");
    Assert(consultaValidation.Message.Contains("data passada"), "Erro de data passada nao retornado.");

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
        Cpf = "123.456.789-09",
        Email = "cliente.workflow@example.com",
        Telefone = "(11) 99999-9999",
        Status = "Ativo"
    };
    await clienteWorkflow.SaveAsync(cliente);

    var clientes = await clienteWorkflow.LoadAsync();
    Assert(clientes.Count == 1, "Cliente nao foi salvo pelo workflow.");
    Assert(clienteWorkflow.MatchesSearch(clientes[0], "workflow"), "Busca por nome falhou.");
    Assert(clienteWorkflow.MatchesSearch(clientes[0], "12345678909"), "Busca por CPF falhou.");

    var duplicado = new Cliente
    {
        Nome = "Cliente Duplicado",
        Cpf = "12345678909",
        Status = "Ativo"
    };
    await AssertThrowsAsync<InvalidOperationException>(
        () => clienteWorkflow.SaveAsync(duplicado),
        "CPF duplicado nao foi bloqueado pelo workflow.");

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
    Assert(clientes.Count == 0, "Delete de cliente pelo workflow falhou.");
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
