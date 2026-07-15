using AgendamentoWpfApp;
using AgendamentoWpfApp.Data;
using AgendamentoWpfApp.Data.Stores;
using AgendamentoWpfApp.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

if (args.Any(a => a.Equals("--seed-large", StringComparison.OrdinalIgnoreCase)))
{
    await SeedLargeAsync();
    return 0;
}

var tempFolder = Path.Combine(Path.GetTempPath(), "RetaguardaAgendamentoSmoke", Guid.NewGuid().ToString("N"));
var filePath = Path.Combine(tempFolder, "agenda.sqlite");

try
{
    var store = new ClienteLocalStore(filePath);
    var consultaStore = new ConsultaLocalStore(filePath);
    var profissionalStore = new ProfissionalSalaLocalStore(filePath);
    await store.MigrateAsync();
    await consultaStore.MigrateAsync();
    await profissionalStore.MigrateAsync();

    var cliente = new Cliente
    {
        Nome = "Cliente Smoke",
        Empresa = "Empresa Smoke",
        Cargo = "Vigilante",
        Cpf = "123.456.789-09",
        Email = "cliente.smoke@example.com",
        Telefone = "(11) 99999-9999",
        DataNascimento = new DateTime(1995, 5, 20),
        Endereco = "Rua Teste, 100",
        Bairro = "Centro",
        Cidade = "Sao Paulo",
        Status = "Ativo",
        Observacoes = "Smoke de persistencia local"
    };

    await store.SaveAsync(cliente);

    var carregados = await store.LoadAsync();
    Assert(carregados.Count == 1, "Quantidade de clientes carregados invalida.");

    var salvo = carregados[0];
    Assert(salvo.Nome == "Cliente Smoke", "Nome nao foi persistido.");
    Assert(salvo.Cpf == "12345678909", "CPF nao foi persistido.");
    Assert(salvo.CpfFormatado == "123.456.789-09", "CPF formatado invalido.");
    Assert(salvo.Telefone == "11999999999", "Telefone nao foi normalizado.");
    Assert(salvo.Email == "cliente.smoke@example.com", "E-mail nao foi persistido.");

    // CPF duplicado e permitido: a base legada importada tem repeticoes
    // (mesma regra coberta pela suite de testes do workflow).
    var duplicado = new Cliente
    {
        Nome = "Cliente Mesmo CPF",
        Empresa = "Empresa Smoke",
        Cargo = "Vigilante",
        Cpf = "12345678909",
        Status = "Ativo"
    };
    await store.SaveAsync(duplicado);
    Assert((await store.LoadAsync()).Count == 2, "CPF duplicado deveria ser permitido para dados legados.");

    var sala1 = new ProfissionalSala
    {
        Nome = "Sala 1",
        Tipo = "Sala",
        EspecialidadeFuncao = "Atendimento",
        Ativo = true
    };
    var sala2 = new ProfissionalSala
    {
        Nome = "Sala 2",
        Tipo = "Sala",
        EspecialidadeFuncao = "Atendimento",
        Ativo = true
    };

    await profissionalStore.SaveAsync(sala1);
    await profissionalStore.SaveAsync(sala2);
    var profissionais = await profissionalStore.LoadAsync();
    Assert(profissionais.Count == 2, "Profissionais/salas nao foram persistidos.");

    var consulta = new Consulta
    {
        ClienteIdLocal = salvo.IdLocal,
        ClienteNome = salvo.Nome,
        DataConsulta = new DateTime(2026, 7, 10),
        Horario = "14:00",
        Local = "Sala 1",
        ProfissionalSala = sala1.Nome,
        ProfissionalSalaIdLocal = sala1.IdLocal,
        Status = "Agendada",
        Observacoes = "Consulta criada no smoke"
    };

    await consultaStore.SaveAsync(consulta);
    var consultasDoDia = await consultaStore.LoadByDateAsync(new DateTime(2026, 7, 10));
    Assert(consultasDoDia.Count == 1, "Consulta nao foi carregada no dia esperado.");
    Assert(consultasDoDia[0].ClienteNome == "Cliente Smoke", "Cliente da consulta nao foi persistido.");
    Assert(consultasDoDia[0].DataHoraTexto == "10/07/2026 14:00", "Texto de data/hora da consulta invalido.");

    var conflito = new Consulta
    {
        ClienteIdLocal = salvo.IdLocal,
        ClienteNome = "Outro Cliente",
        DataConsulta = new DateTime(2026, 7, 10),
        Horario = "14:00",
        Local = "Sala 1",
        ProfissionalSala = sala1.Nome,
        ProfissionalSalaIdLocal = sala1.IdLocal,
        Status = "Agendada"
    };

    var bloqueouConflito = false;
    try
    {
        await consultaStore.SaveAsync(conflito);
    }
    catch (InvalidOperationException)
    {
        bloqueouConflito = true;
    }

    Assert(bloqueouConflito, "Conflito de horario nao foi bloqueado.");

    var mesmaHoraOutraSala = new Consulta
    {
        ClienteIdLocal = salvo.IdLocal,
        ClienteNome = "Outro Cliente",
        DataConsulta = new DateTime(2026, 7, 10),
        Horario = "14:00",
        Local = "Sala 2",
        ProfissionalSala = sala2.Nome,
        ProfissionalSalaIdLocal = sala2.IdLocal,
        Status = "Agendada"
    };

    await consultaStore.SaveAsync(mesmaHoraOutraSala);
    consultasDoDia = await consultaStore.LoadByDateAsync(new DateTime(2026, 7, 10));
    Assert(consultasDoDia.Count == 2, "Mesma hora em sala diferente deveria ser permitida.");

    salvo.Telefone = "11888887777";
    salvo.Status = "Concluido";
    await store.SaveAsync(salvo);

    var recarregado = (await store.LoadAsync()).Single(c => c.IdLocal == salvo.IdLocal);
    Assert(recarregado.Telefone == "11888887777", "Atualizacao de telefone nao foi persistida.");
    Assert(recarregado.Status == "Concluido", "Atualizacao de status nao foi persistida.");

    await store.MarkDeletedAsync(recarregado);
    var ativos = await store.LoadAsync();
    Assert(ativos.Count == 1 && ativos[0].IdLocal == duplicado.IdLocal, "Exclusao logica nao removeu cliente da listagem ativa.");

    var todos = await store.LoadAsync(incluirExcluidos: true);
    Assert(todos.Count == 2 && todos.Single(c => c.IdLocal == salvo.IdLocal).Excluido, "Exclusao logica nao foi persistida.");

    await consultaStore.MarkDeletedAsync(consultasDoDia[0]);
    var consultasAtivas = await consultaStore.LoadByDateAsync(new DateTime(2026, 7, 10));
    Assert(consultasAtivas.Count == 1, "Exclusao logica da consulta removeu mais consultas do que deveria.");

    Console.WriteLine("Smoke WPF SQLite concluido com sucesso.");
    Console.WriteLine($"Banco temporario: {filePath}");
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

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static async Task SeedLargeAsync()
{
    var database = new AgendaDatabase();
    await database.MigrateAsync();

    await using var context = database.CreateContext();
    var profissionais = await EnsureSeedProfissionaisAsync(context);
    await EnsureSeedClientesEConsultasAsync(context, profissionais);

    var totalClientes = await context.Clientes.CountAsync(c => !c.Excluido);
    var totalProfissionais = await context.ProfissionaisSalas.CountAsync(p => !p.Excluido);
    var totalConsultas = await context.Consultas.CountAsync(c => !c.Excluido);

    Console.WriteLine("Seed grande concluido com sucesso.");
    Console.WriteLine($"Banco local: {AgendaDbContext.DefaultDatabasePath}");
    Console.WriteLine($"Clientes ativos: {totalClientes}");
    Console.WriteLine($"Profissionais/salas ativos: {totalProfissionais}");
    Console.WriteLine($"Consultas ativas: {totalConsultas}");
}

static async Task<List<ProfissionalSala>> EnsureSeedProfissionaisAsync(AgendaDbContext context)
{
    var seeds = new[]
    {
        new ProfissionalSala
        {
            IdLocal = "seed-profissional-001",
            Nome = "Dra. Helena Martins",
            Tipo = "Profissional",
            EspecialidadeFuncao = "Psicologa clinica",
            Telefone = "(34) 98810-1201",
            Email = "helena.martins@sparkcore.test",
            Observacoes = "Profissional gerada para teste de carga.",
            Ativo = true
        },
        new ProfissionalSala
        {
            IdLocal = "seed-profissional-002",
            Nome = "Dr. Rafael Azevedo",
            Tipo = "Profissional",
            EspecialidadeFuncao = "Neuropsicologo",
            Telefone = "(34) 98810-1202",
            Email = "rafael.azevedo@sparkcore.test",
            Observacoes = "Profissional gerado para teste de carga.",
            Ativo = true
        }
    };

    foreach (var seed in seeds)
    {
        var existing = await context.ProfissionaisSalas.FirstOrDefaultAsync(p => p.IdLocal == seed.IdLocal);
        if (existing == null)
        {
            await context.ProfissionaisSalas.AddAsync(seed);
        }
        else
        {
            existing.Nome = seed.Nome;
            existing.Tipo = seed.Tipo;
            existing.EspecialidadeFuncao = seed.EspecialidadeFuncao;
            existing.Telefone = seed.Telefone;
            existing.Email = seed.Email;
            existing.Observacoes = seed.Observacoes;
            existing.Ativo = true;
            existing.Excluido = false;
            existing.AtualizadoEm = DateTime.Now;
        }
    }

    await context.SaveChangesAsync();
    return await context.ProfissionaisSalas
        .Where(p => seeds.Select(s => s.IdLocal).Contains(p.IdLocal))
        .OrderBy(p => p.IdLocal)
        .ToListAsync();
}

static async Task EnsureSeedClientesEConsultasAsync(AgendaDbContext context, IReadOnlyList<ProfissionalSala> profissionais)
{
    var firstNames = new[]
    {
        "Ana", "Bruno", "Carla", "Diego", "Eduarda", "Felipe", "Gabriela", "Henrique", "Isabela", "Joao",
        "Karina", "Leonardo", "Mariana", "Nicolas", "Olivia", "Pedro", "Renata", "Samuel", "Tatiane", "Victor",
        "Amanda", "Caio", "Daniela", "Erick", "Fernanda", "Gustavo", "Helena", "Igor", "Juliana", "Lucas",
        "Larissa", "Marcelo", "Natali", "Otavio", "Paula", "Rafael", "Sofia", "Thiago", "Vanessa", "Yasmin"
    };
    var middleNames = new[]
    {
        "Aparecida", "Cristina", "Eduardo", "Fernandes", "Gabriel", "Henrique", "Luiza", "Marques", "Regina", "Vitoria",
        "Almeida", "Batista", "Camila", "Dias", "Estevao", "Freitas", "Gomes", "Lopes", "Moreira", "Teixeira"
    };
    var lastNames = new[]
    {
        "Silva", "Santos", "Oliveira", "Souza", "Rodrigues", "Ferreira", "Alves", "Pereira", "Lima", "Gomes",
        "Ribeiro", "Carvalho", "Almeida", "Martins", "Araujo", "Barbosa", "Cardoso", "Mendes", "Rocha", "Nogueira"
    };
    var bairros = new[] { "Centro", "Santa Monica", "Saraiva", "Tibery", "Morumbi", "Brasil", "Lidice", "Martins", "Planalto", "Jardim Europa" };
    var cidades = new[] { "Uberlandia", "Araguari", "Uberaba", "Patos de Minas", "Ituiutaba" };

    var startDate = DateTime.Today.AddDays(1);
    var horarios = new[] { "08:00", "08:30", "09:00", "09:30", "10:00", "10:30", "11:00", "13:00", "13:30", "14:00", "14:30", "15:00", "15:30", "16:00", "16:30", "17:00" };

    for (var i = 1; i <= 1000; i++)
    {
        var idLocal = $"seed-cliente-{i:0000}";
        var cpf = GerarCpfValido(700000000 + i);
        var first = firstNames[(i - 1) % firstNames.Length];
        var middle = middleNames[(i * 3) % middleNames.Length];
        var last = lastNames[(i * 7) % lastNames.Length];
        var secondLast = lastNames[(i * 11) % lastNames.Length];
        var nome = $"{first} {middle} {last} {secondLast}";

        var cliente = await context.Clientes.FirstOrDefaultAsync(c => c.IdLocal == idLocal);
        if (cliente == null)
        {
            cliente = new Cliente { IdLocal = idLocal };
            await context.Clientes.AddAsync(cliente);
        }

        cliente.Nome = nome;
        cliente.Cpf = cpf;
        cliente.Email = $"{NormalizeEmail(first)}.{NormalizeEmail(last)}.{i:0000}@exemplo.test";
        cliente.Telefone = $"(34) 9{92000000 + i:00000000}";
        cliente.DataNascimento = new DateTime(1970 + (i % 32), ((i - 1) % 12) + 1, ((i - 1) % 28) + 1);
        cliente.Endereco = $"Rua {last} {middle}, {100 + i}";
        cliente.Bairro = bairros[i % bairros.Length];
        cliente.Cidade = cidades[i % cidades.Length];
        cliente.Status = "Ativo";
        cliente.Observacoes = "Cliente gerado para teste de carga do SparkCore.";
        cliente.Excluido = false;
        cliente.AtualizadoEm = DateTime.Now;

        var profissional = profissionais[(i - 1) % profissionais.Count];
        var consultaIdLocal = $"seed-consulta-{i:0000}";
        var slotIndex = i - 1;
        var dataConsulta = startDate.AddDays(slotIndex / (horarios.Length * profissionais.Count));
        var horario = horarios[(slotIndex / profissionais.Count) % horarios.Length];

        var consulta = await context.Consultas.FirstOrDefaultAsync(c => c.IdLocal == consultaIdLocal);
        if (consulta == null)
        {
            consulta = new Consulta { IdLocal = consultaIdLocal };
            await context.Consultas.AddAsync(consulta);
        }

        consulta.ClienteIdLocal = cliente.IdLocal;
        consulta.ClienteNome = cliente.Nome;
        consulta.DataConsulta = dataConsulta.Date;
        consulta.Horario = horario;
        consulta.Local = profissional.Nome;
        consulta.ProfissionalSala = profissional.Nome;
        consulta.ProfissionalSalaIdLocal = profissional.IdLocal;
        consulta.Status = "Agendada";
        consulta.Observacoes = "Consulta gerada para teste de carga.";
        consulta.Excluido = false;
        consulta.AtualizadoEm = DateTime.Now;

        if (i % 100 == 0)
            await context.SaveChangesAsync();
    }

    await context.SaveChangesAsync();
}

static string GerarCpfValido(int seed)
{
    var baseDigits = (seed % 1000000000).ToString("000000000");
    var firstDigit = CalcularCpfDigito(baseDigits, 10);
    var secondDigit = CalcularCpfDigito(baseDigits + firstDigit, 11);
    return baseDigits + firstDigit + secondDigit;
}

static int CalcularCpfDigito(string digits, int pesoInicial)
{
    var soma = 0;
    for (var i = 0; i < digits.Length; i++)
        soma += (digits[i] - '0') * (pesoInicial - i);

    var resto = soma % 11;
    return resto < 2 ? 0 : 11 - resto;
}

static string NormalizeEmail(string value)
{
    return value
        .ToLowerInvariant()
        .Replace("ç", "c")
        .Replace("á", "a")
        .Replace("é", "e")
        .Replace("í", "i")
        .Replace("ó", "o")
        .Replace("ú", "u")
        .Replace("ã", "a")
        .Replace("õ", "o");
}
