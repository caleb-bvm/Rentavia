namespace Rentavia.Domain.Entities;

/// <summary>Sucursal de la agencia. Si está inactiva, sus vehículos no se ofrecen.</summary>
public class Sucursal
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public bool Activa { get; set; } = true;
}
