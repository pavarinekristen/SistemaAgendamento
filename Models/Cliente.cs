using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AgendamentoWpfApp.Services.Validation;

namespace AgendamentoWpfApp.Models;

[Table("CLIENTES")]
internal sealed class Cliente : IRegistroSincronizavel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(36)]
    public string IdLocal { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [MaxLength(160)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(11)]
    public string Cpf { get; set; } = string.Empty;

    [MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Empresa { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Escolaridade { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Cargo { get; set; } = string.Empty;

    [MaxLength(40)]
    public string EstadoCivil { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Naturalidade { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Rg { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Sexo { get; set; } = string.Empty;

    [MaxLength(20)]
    public string TipoEndereco { get; set; } = "Particular";

    [MaxLength(11)]
    public string Telefone { get; set; } = string.Empty;

    public DateTime? DataNascimento { get; set; }

    [MaxLength(240)]
    public string Endereco { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Bairro { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Cidade { get; set; } = string.Empty;

    [MaxLength(120)]
    public string LocalAtendimento { get; set; } = string.Empty;

    public DateTime? DataAgendamento { get; set; }

    [MaxLength(10)]
    public string HorarioAgendamento { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Status { get; set; } = "Ativo";

    public string Observacoes { get; set; } = string.Empty;

    // Blob de pesquisa (caixa alta, sem acento) mantido pelo ClienteLocalStore
    // ao salvar; a coluna e criada/backfillada em AgendaDatabase.MigrateAsync.
    public string PesquisaNormalizada { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public DateTime AtualizadoEm { get; set; } = DateTime.Now;
    public DateTime? SincronizadoEm { get; set; }
    public bool Excluido { get; set; }

    [MaxLength(64)]
    public string HashSincronizacao { get; set; } = string.Empty;

    [NotMapped]
    public string CpfFormatado => InputNormalizer.FormatCpf(Cpf);

    [NotMapped]
    public string TelefoneFormatado => InputNormalizer.FormatTelefone(Telefone);

    [NotMapped]
    public string IdTexto => Id > 0 ? Id.ToString("0000") : "-";

    [NotMapped]
    public string DataAgendamentoTexto => DataAgendamento?.ToString("dd/MM/yyyy") ?? "";

    [NotMapped]
    public string DataNascimentoTexto => DataNascimento?.ToString("dd/MM/yyyy") ?? "";

    [NotMapped]
    public string ProximoHorarioTexto
    {
        get
        {
            var data = DataAgendamentoTexto;
            if (string.IsNullOrWhiteSpace(data) && string.IsNullOrWhiteSpace(HorarioAgendamento))
                return "";

            return $"{data} {HorarioAgendamento}".Trim();
        }
    }
}
