namespace Rentavia.Domain.Entities;

/// <summary>Cliente de la agencia. No tiene acceso al sistema; lo registran los empleados.</summary>
public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Licencia { get; set; } = string.Empty;
    public DateOnly VencimientoLicencia { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public DateTimeOffset FechaRegistroUtc { get; set; }
    public byte[] Version { get; set; } = [];
    public bool Activo { get; set; } = true;
}
