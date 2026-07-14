using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AgendamentoWpfApp.Services.Validation;

namespace AgendamentoWpfApp.Models;

[Table("PROFISSIONAIS_SALAS")]
internal sealed class ProfissionalSala : IRegistroSincronizavel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(36)]
    public string IdLocal { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [MaxLength(140)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Tipo { get; set; } = "Profissional";

    [MaxLength(120)]
    public string EspecialidadeFuncao { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Telefone { get; set; } = string.Empty;

    [MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    public string Observacoes { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public DateTime AtualizadoEm { get; set; } = DateTime.Now;
    public DateTime? SincronizadoEm { get; set; }
    public bool Excluido { get; set; }

    [MaxLength(64)]
    public string HashSincronizacao { get; set; } = string.Empty;

    [NotMapped]
    public string NomeExibicao => string.IsNullOrWhiteSpace(EspecialidadeFuncao)
        ? $"{Tipo}: {Nome}"
        : $"{Tipo}: {Nome} - {EspecialidadeFuncao}";

    [NotMapped]
    public string TelefoneFormatado => InputNormalizer.FormatTelefone(Telefone);
}
