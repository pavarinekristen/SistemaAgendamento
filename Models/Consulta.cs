using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgendamentoWpfApp.Models;

[Table("CONSULTAS")]
internal sealed class Consulta
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(36)]
    public string IdLocal { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [MaxLength(36)]
    public string ClienteIdLocal { get; set; } = string.Empty;

    [Required]
    [MaxLength(160)]
    public string ClienteNome { get; set; } = string.Empty;

    public DateTime DataConsulta { get; set; } = DateTime.Today;

    [Required]
    [MaxLength(10)]
    public string Horario { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Local { get; set; } = string.Empty;

    [MaxLength(120)]
    public string ProfissionalSala { get; set; } = string.Empty;

    [MaxLength(36)]
    public string ProfissionalSalaIdLocal { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Status { get; set; } = "Agendada";

    public string Observacoes { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public DateTime AtualizadoEm { get; set; } = DateTime.Now;
    public DateTime? SincronizadoEm { get; set; }
    public bool Excluido { get; set; }

    [MaxLength(64)]
    public string HashSincronizacao { get; set; } = string.Empty;

    [NotMapped]
    public string DataTexto => DataConsulta.ToString("dd/MM/yyyy");

    [NotMapped]
    public string DataHoraTexto => $"{DataTexto} {Horario}".Trim();
}
