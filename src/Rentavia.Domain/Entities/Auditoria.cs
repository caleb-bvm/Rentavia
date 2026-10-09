namespace Rentavia.Domain.Entities;

/// <summary>Evento de una operación sensible: quién la hizo, qué acción y sobre qué registro.</summary>
public class Auditoria
{
    public int Id { get; set; }
    public DateTimeOffset FechaUtc { get; set; } = DateTimeOffset.UtcNow;
    public string UsuarioId { get; set; } = "";
    public string Accion { get; set; } = "";
    public string Entidad { get; set; } = "";
    public int RegistroId { get; set; }
    public string? RegistroClave { get; set; }
}
